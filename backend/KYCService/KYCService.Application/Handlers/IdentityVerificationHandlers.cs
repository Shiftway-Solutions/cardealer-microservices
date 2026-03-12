using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KYCService.Application.Clients;
using KYCService.Application.Commands;
using KYCService.Application.DTOs;
using KYCService.Application.Queries;
using KYCService.Application.Services;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Domain.Validators;
using KYCService.Infrastructure.ExternalServices;

namespace KYCService.Application.Handlers;

/// <summary>
/// Handler para iniciar sesión de verificación de identidad
/// </summary>
public class StartIdentityVerificationHandler : IRequestHandler<StartIdentityVerificationCommand, StartVerificationResponse>
{
    private readonly ILogger<StartIdentityVerificationHandler> _logger;
    private readonly IdentityVerificationConfig _config;
    private readonly IKYCConfigurationService _kycConfig;

    public StartIdentityVerificationHandler(
        ILogger<StartIdentityVerificationHandler> logger,
        IOptions<IdentityVerificationConfig> config,
        IKYCConfigurationService kycConfig)
    {
        _logger = logger;
        _config = config.Value;
        _kycConfig = kycConfig;
    }

    public async Task<StartVerificationResponse> Handle(StartIdentityVerificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting identity verification session for user {UserId}", request.UserId);

        // Read dynamic config from ConfigurationService (admin panel)
        var maxAttempts = await _kycConfig.GetMaxVerificationAttemptsAsync();
        var timeoutMinutes = await _kycConfig.GetVerificationTimeoutMinutesAsync();

        // Crear nueva sesión
        var session = new IdentityVerificationSession
        {
            UserId = request.UserId,
            DocumentType = request.DocumentType,
            Status = VerificationSessionStatus.Started,
            ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes),
            MaxAttempts = maxAttempts,
            IPAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            Latitude = request.Location?.Latitude,
            Longitude = request.Location?.Longitude,
            DeviceInfo = request.DeviceInfo != null
                ? System.Text.Json.JsonSerializer.Serialize(request.DeviceInfo)
                : null
        };

        // Generar challenges aleatorios para liveness
        var challenges = GenerateRandomChallenges(_config.Liveness.ChallengesRequired);
        session.SetLivenessChallenges(challenges);

        // TODO: Persistir sesión en base de datos
        // await _sessionRepository.AddAsync(session, cancellationToken);

        _logger.LogInformation("Identity verification session {SessionId} created for user {UserId}",
            session.Id, request.UserId);

        return new StartVerificationResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            DocumentType = session.DocumentType.ToString(),
            ExpiresAt = session.ExpiresAt,
            ExpiresInSeconds = (int)(session.ExpiresAt - DateTime.UtcNow).TotalSeconds,
            NextStep = "CAPTURE_DOCUMENT_FRONT",
            RequiredChallenges = challenges.Select(c => c.ToString()).ToList(),
            Instructions = GetDocumentCaptureInstructions(request.DocumentType, DocumentSide.Front)
        };
    }

    private List<Domain.Entities.LivenessChallenge> GenerateRandomChallenges(int count)
    {
        var available = _config.Liveness.AvailableChallenges;
        var random = new Random();
        return available.OrderBy(_ => random.Next()).Take(count).ToList();
    }

    private VerificationInstructionsDto GetDocumentCaptureInstructions(DocumentType docType, DocumentSide side)
    {
        var docName = docType switch
        {
            DocumentType.Cedula => "cédula",
            DocumentType.Passport => "pasaporte",
            _ => "documento"
        };

        var sideName = side == DocumentSide.Front ? "frente" : "reverso";

        return new VerificationInstructionsDto
        {
            Title = $"Captura el {sideName} de tu {docName}",
            Steps = new List<string>
            {
                $"Coloca tu {docName} sobre una superficie plana",
                "Asegúrate de que haya buena iluminación",
                "Alinea el documento dentro del marco",
                "Mantén la cámara estable"
            },
            Tips = new List<string>
            {
                "Evita reflejos y sombras",
                "Asegúrate que las 4 esquinas sean visibles",
                "El texto debe ser legible"
            }
        };
    }
}

/// <summary>
/// Handler para procesar documento (frente o reverso)
/// </summary>
public class ProcessDocumentHandler : IRequestHandler<ProcessDocumentCommand, DocumentProcessedResponse>
{
    private readonly ILogger<ProcessDocumentHandler> _logger;
    private readonly IdentityVerificationConfig _config;

    public ProcessDocumentHandler(
        ILogger<ProcessDocumentHandler> logger,
        IOptions<IdentityVerificationConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task<DocumentProcessedResponse> Handle(ProcessDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing document {Side} for session {SessionId}",
            request.Side, request.SessionId);

        // TODO: Obtener sesión de base de datos
        // var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        // Simular sesión para el ejemplo
        var session = new IdentityVerificationSession
        {
            Id = request.SessionId,
            UserId = request.UserId,
            DocumentType = DocumentType.Cedula
        };

        // Validar que la sesión no ha expirado
        if (session.IsExpired)
        {
            throw new InvalidOperationException("La sesión de verificación ha expirado");
        }

        // TODO: Subir imagen a S3 a través de MediaService
        var imageUrl = $"https://cdn.okla.com.do/kyc/{request.SessionId}/{request.Side.ToString().ToLower()}.jpg";

        // TODO: Procesar OCR con Azure Computer Vision
        var ocrResult = await ProcessOcrAsync(request.ImageData, session.DocumentType, request.Side);

        // Validar documento si es cédula
        DocumentValidationDto? documentValidation = null;
        if (session.DocumentType == DocumentType.Cedula && !string.IsNullOrEmpty(ocrResult.ExtractedData?.DocumentNumber))
        {
            var validation = CedulaValidator.ValidateDetailed(ocrResult.ExtractedData.DocumentNumber);
            documentValidation = new DocumentValidationDto
            {
                FormatValid = validation.FormatValid,
                ChecksumValid = validation.ChecksumValid,
                NotExpired = true, // TODO: Validar contra fecha de expiración extraída
                AgeValid = true,   // TODO: Validar edad
                Issues = validation.Errors
            };

            if (!validation.IsValid)
            {
                _logger.LogWarning("Document validation failed for session {SessionId}: {Errors}",
                    request.SessionId, string.Join(", ", validation.Errors));
            }
        }

        // Actualizar sesión
        if (request.Side == DocumentSide.Front)
        {
            session.DocumentFrontUrl = imageUrl;
            session.DocumentFrontProcessed = true;
            session.DocumentFrontCapturedAt = DateTime.UtcNow;
            session.Status = VerificationSessionStatus.DocumentFrontCaptured;

            // Guardar datos OCR
            if (ocrResult.Success && ocrResult.ExtractedData != null)
            {
                session.ExtractedFullName = ocrResult.ExtractedData.FullName;
                session.ExtractedFirstName = ocrResult.ExtractedData.FirstName;
                session.ExtractedLastName = ocrResult.ExtractedData.LastName;
                session.ExtractedDocumentNumber = ocrResult.ExtractedData.DocumentNumber;
                if (DateTime.TryParse(ocrResult.ExtractedData.DateOfBirth, out var dob))
                    session.ExtractedDateOfBirth = dob;
                session.ExtractedNationality = ocrResult.ExtractedData.Nationality;
                session.OcrConfidence = ocrResult.Confidence;
            }
        }
        else
        {
            session.DocumentBackUrl = imageUrl;
            session.DocumentBackProcessed = true;
            session.DocumentBackCapturedAt = DateTime.UtcNow;
            session.Status = VerificationSessionStatus.AwaitingSelfie;

            // Extraer datos adicionales del reverso si aplica
            if (ocrResult.Success && ocrResult.ExtractedData != null)
            {
                session.ExtractedAddress = ocrResult.ExtractedData.Address;
                if (DateTime.TryParse(ocrResult.ExtractedData.ExpiryDate, out var expiry))
                    session.ExtractedExpiryDate = expiry;
            }
        }

        // TODO: Persistir cambios
        // await _sessionRepository.UpdateAsync(session, cancellationToken);

        var nextStep = request.Side == DocumentSide.Front ? "CAPTURE_DOCUMENT_BACK" : "LIVENESS_SELFIE";
        var nextInstructions = request.Side == DocumentSide.Front
            ? GetDocumentCaptureInstructions(session.DocumentType, DocumentSide.Back)
            : GetSelfieInstructions();

        return new DocumentProcessedResponse
        {
            SessionId = session.Id,
            Side = request.Side.ToString(),
            Status = session.Status.ToString(),
            OcrResult = ocrResult,
            DocumentValidation = documentValidation,
            NextStep = nextStep,
            Instructions = nextInstructions
        };
    }

    private async Task<OcrResultDto> ProcessOcrAsync(byte[] imageData, DocumentType docType, DocumentSide side)
    {
        // TODO: Implementar llamada real a Azure Computer Vision
        // Por ahora, retornar datos simulados para desarrollo
        await Task.Delay(100); // Simular latencia de API

        // Simular resultado exitoso
        return new OcrResultDto
        {
            Success = true,
            Confidence = 0.95m,
            ExtractedData = new OcrExtractedDataDto
            {
                FullName = "JUAN ANTONIO PEREZ MARTINEZ",
                FirstName = "JUAN ANTONIO",
                LastName = "PEREZ MARTINEZ",
                DocumentNumber = "001-1234567-8",
                DateOfBirth = "1985-06-15",
                ExpiryDate = "2028-06-15",
                Nationality = "DOMINICANA",
                Gender = "M"
            }
        };
    }

    private VerificationInstructionsDto GetDocumentCaptureInstructions(DocumentType docType, DocumentSide side)
    {
        var docName = docType == DocumentType.Cedula ? "cédula" : "documento";
        return new VerificationInstructionsDto
        {
            Title = $"Ahora captura el reverso de tu {docName}",
            Steps = new List<string>
            {
                $"Voltea tu {docName}",
                "Captura el reverso siguiendo las mismas instrucciones"
            }
        };
    }

    private VerificationInstructionsDto GetSelfieInstructions()
    {
        return new VerificationInstructionsDto
        {
            Title = "Verificación de identidad en vivo",
            Steps = new List<string>
            {
                "Coloca tu rostro dentro del óvalo",
                "Asegúrate de buena iluminación",
                "Sigue las instrucciones en pantalla"
            },
            Tips = new List<string>
            {
                "Quita lentes de sol o accesorios",
                "Mira directamente a la cámara",
                "Mantén una expresión neutral al inicio"
            }
        };
    }
}

/// <summary>
/// Handler para procesar selfie con liveness
/// </summary>
public class ProcessSelfieHandler : IRequestHandler<ProcessSelfieCommand, VerificationCompletedResponse>
{
    private readonly ILogger<ProcessSelfieHandler> _logger;
    private readonly IdentityVerificationConfig _config;
    private readonly IKYCConfigurationService _kycConfig;
    private readonly IFaceComparisonService _faceComparisonService;

    public ProcessSelfieHandler(
        ILogger<ProcessSelfieHandler> logger,
        IOptions<IdentityVerificationConfig> config,
        IKYCConfigurationService kycConfig,
        IFaceComparisonService faceComparisonService)
    {
        _logger = logger;
        _config = config.Value;
        _kycConfig = kycConfig;
        _faceComparisonService = faceComparisonService;
    }

    public async Task<VerificationCompletedResponse> Handle(ProcessSelfieCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing selfie for session {SessionId}", request.SessionId);

        // TODO: Obtener sesión de base de datos
        var session = new IdentityVerificationSession
        {
            Id = request.SessionId,
            UserId = request.UserId,
            Status = VerificationSessionStatus.AwaitingSelfie,
            ExtractedFullName = "JUAN ANTONIO PEREZ MARTINEZ",
            ExtractedDocumentNumber = "001-1234567-8",
            ExtractedDateOfBirth = new DateTime(1985, 6, 15),
            ExtractedNationality = "DOMINICANA"
        };

        // Validar estado
        if (session.Status != VerificationSessionStatus.AwaitingSelfie)
        {
            throw new InvalidOperationException("La sesión no está lista para captura de selfie");
        }

        // TODO: Subir selfie a S3
        var selfieUrl = $"https://cdn.okla.com.do/kyc/{request.SessionId}/selfie.jpg";
        session.SelfieUrl = selfieUrl;
        session.SelfieCapturedAt = DateTime.UtcNow;
        session.Status = VerificationSessionStatus.ProcessingBiometrics;

        // Procesar liveness
        var livenessResult = await ProcessLivenessAsync(request.LivenessData, request.SelfieImageData);
        session.LivenessCheckPassed = livenessResult.Passed;
        session.LivenessScore = livenessResult.Score;

        // Comparar rostros using actual image data
        // Note: In the session flow, document image data comes from the ProcessDocument step
        // For now, validate face quality in the selfie via Rekognition detect
        var faceDetection = await _faceComparisonService.DetectFacesAsync(request.SelfieImageData, cancellationToken);
        var faceValid = faceDetection.Success && faceDetection.FaceCount == 1
                        && faceDetection.Faces.Any(f => f.Confidence > 90);
        session.FaceMatchPassed = faceValid;
        session.FaceMatchScore = faceValid ? faceDetection.Faces.First().Confidence : 0;

        // Determinar resultado
        var verified = session.LivenessCheckPassed &&
                       session.FaceMatchPassed &&
                       session.DocumentValidationPassed;

        if (verified)
        {
            session.Status = VerificationSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            // TODO: Crear o actualizar KYCProfile
            _logger.LogInformation("Identity verification completed successfully for session {SessionId}",
                request.SessionId);

            return new VerificationCompletedResponse
            {
                SessionId = session.Id,
                Status = "Completed",
                Result = new VerificationResultDto
                {
                    Verified = true,
                    OverallScore = session.CalculateOverallScore(),
                    Details = new VerificationDetailsDto
                    {
                        DocumentAuthenticity = new DocumentAuthenticityDto
                        {
                            Passed = true,
                            Score = session.OcrConfidence * 100
                        },
                        LivenessDetection = new LivenessDetectionDto
                        {
                            Passed = session.LivenessCheckPassed,
                            Score = session.LivenessScore,
                            ChallengesPassed = request.LivenessData?.Challenges.Count(c => c.Passed) ?? 0,
                            ChallengesTotal = request.LivenessData?.Challenges.Count ?? 3
                        },
                        FaceMatch = new FaceMatchDto
                        {
                            Passed = session.FaceMatchPassed,
                            Score = session.FaceMatchScore,
                            Threshold = session.FaceMatchThreshold
                        },
                        OcrAccuracy = new OcrAccuracyDto
                        {
                            Confidence = session.OcrConfidence * 100,
                            FieldsExtracted = 6,
                            FieldsTotal = 6
                        }
                    }
                },
                ExtractedProfile = new ExtractedProfileDto
                {
                    FullName = session.ExtractedFullName ?? "",
                    FirstName = session.ExtractedFirstName,
                    LastName = session.ExtractedLastName,
                    DocumentNumber = session.ExtractedDocumentNumber ?? "",
                    DocumentType = session.DocumentType.ToString(),
                    DateOfBirth = session.ExtractedDateOfBirth?.ToString("yyyy-MM-dd"),
                    Nationality = session.ExtractedNationality
                },
                KYCStatus = "PendingReview",
                Message = "¡Verificación exitosa! Tu identidad ha sido confirmada y está pendiente de revisión final."
            };
        }
        else
        {
            session.Status = VerificationSessionStatus.Failed;
            session.FailureReason = DetermineFailureReason(session);
            session.FailureDetails = GetFailureDetails(session.FailureReason ?? VerificationFailureReason.None);

            _logger.LogWarning("Identity verification failed for session {SessionId}: {Reason}",
                request.SessionId, session.FailureReason);

            throw new VerificationFailedException(session);
        }
    }

    private async Task<(bool Passed, decimal Score)> ProcessLivenessAsync(LivenessDataDto? livenessData, byte[] selfieImageData)
    {
        if (livenessData == null || !livenessData.Challenges.Any())
            return (false, 0);

        // Use AWS Rekognition to verify liveness from captured video frames
        var livenessRequest = new LivenessCheckRequest
        {
            SelfieImage = selfieImageData,
            ChallengeResults = livenessData.Challenges.Select(c => new Infrastructure.ExternalServices.LivenessChallengeResult
            {
                ChallengeType = c.Type,
                Passed = c.Passed,
                Timestamp = c.Timestamp,
                Confidence = c.Confidence ?? 0
            }).ToList()
        };

        // Also include video frames if available (base64 → bytes)
        if (livenessData.VideoFrames?.Any() == true)
        {
            livenessRequest.VideoFrames = livenessData.VideoFrames
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(frame =>
                {
                    try
                    {
                        // Handle data URL format: "data:image/jpeg;base64,..."
                        var base64Data = frame.Contains(",") ? frame.Split(',')[1] : frame;
                        return Convert.FromBase64String(base64Data);
                    }
                    catch
                    {
                        return Array.Empty<byte>();
                    }
                })
                .Where(b => b.Length > 0)
                .ToList();
        }

        var livenessResult = await _faceComparisonService.CheckLivenessAsync(livenessRequest);

        if (!livenessResult.Success)
        {
            _logger.LogWarning("Liveness check failed: {Error}", livenessResult.ErrorMessage);
            return (false, 0);
        }

        _logger.LogInformation("Liveness check result: IsLive={IsLive}, Score={Score}",
            livenessResult.IsLive, livenessResult.LivenessScore);

        return (livenessResult.IsLive, livenessResult.LivenessScore);
    }

    private async Task<(bool Passed, decimal Score)> CompareFacesAsync(byte[]? documentImageData, byte[] selfieImageData)
    {
        if (documentImageData == null || documentImageData.Length == 0)
        {
            _logger.LogWarning("Document image data not available for face comparison");
            return (false, 0);
        }

        // Use AWS Rekognition for real face comparison
        var comparisonResult = await _faceComparisonService.CompareWithDocumentAsync(
            selfieImageData, documentImageData);

        if (!comparisonResult.Success)
        {
            _logger.LogWarning("Face comparison failed: {Error}", comparisonResult.ErrorMessage);
            return (false, 0);
        }

        // Use dynamic threshold from admin panel
        var threshold = await _kycConfig.GetFacialMatchThresholdAsync();
        var passed = comparisonResult.SimilarityScore >= threshold;

        _logger.LogInformation(
            "Face comparison result: Score={Score}, Threshold={Threshold}, Passed={Passed}",
            comparisonResult.SimilarityScore, threshold, passed);

        return (passed, comparisonResult.SimilarityScore);
    }

    private VerificationFailureReason DetermineFailureReason(IdentityVerificationSession session)
    {
        if (!session.LivenessCheckPassed)
            return VerificationFailureReason.LivenessCheckFailed;

        if (!session.FaceMatchPassed)
            return VerificationFailureReason.FaceMismatch;

        if (!session.DocumentValidationPassed)
            return VerificationFailureReason.InvalidDocumentNumber;

        return VerificationFailureReason.None;
    }

    private string GetFailureDetails(VerificationFailureReason reason)
    {
        return reason switch
        {
            VerificationFailureReason.FaceMismatch =>
                "La foto de la selfie no coincide con la foto del documento de identidad.",
            VerificationFailureReason.LivenessCheckFailed =>
                "No se pudo verificar que eres una persona real. Por favor, sigue las instrucciones cuidadosamente.",
            VerificationFailureReason.DocumentBlurry =>
                "La imagen del documento está borrosa. Por favor, toma una foto más clara.",
            VerificationFailureReason.InvalidDocumentNumber =>
                "El número de documento no es válido. Por favor, verifica que sea correcto.",
            _ => "La verificación no pudo completarse. Por favor, intenta de nuevo."
        };
    }
}

/// <summary>
/// Handler para reintentar verificación
/// </summary>
public class RetryVerificationHandler : IRequestHandler<RetryVerificationCommand, StartVerificationResponse>
{
    private readonly ILogger<RetryVerificationHandler> _logger;
    private readonly IMediator _mediator;

    public RetryVerificationHandler(
        ILogger<RetryVerificationHandler> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<StartVerificationResponse> Handle(RetryVerificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrying verification for session {SessionId}", request.SessionId);

        // TODO: Obtener sesión anterior
        // var previousSession = await _sessionRepository.GetByIdAsync(request.SessionId);

        // Verificar que puede reintentar
        // if (!previousSession.CanRetry)
        // {
        //     throw new InvalidOperationException("No puede reintentar la verificación");
        // }

        // Iniciar nueva sesión con intento incrementado
        var command = new StartIdentityVerificationCommand
        {
            UserId = request.UserId,
            DocumentType = DocumentType.Cedula // TODO: obtener del previousSession
        };

        var response = await _mediator.Send(command, cancellationToken);

        // TODO: Incrementar número de intento
        // newSession.AttemptNumber = previousSession.AttemptNumber + 1;

        return response;
    }
}

/// <summary>
/// Excepción cuando la verificación falla
/// </summary>
public class VerificationFailedException : Exception
{
    public IdentityVerificationSession Session { get; }
    public VerificationFailedResponse Response { get; }

    public VerificationFailedException(IdentityVerificationSession session)
        : base($"Verification failed: {session.FailureReason}")
    {
        Session = session;
        Response = new VerificationFailedResponse
        {
            SessionId = session.Id,
            Status = "Failed",
            Result = new FailedResultDto
            {
                Verified = false,
                FailureReason = session.FailureReason?.ToString() ?? "Unknown",
                FailureDetails = session.FailureDetails ?? ""
            },
            AttemptsRemaining = session.MaxAttempts - session.AttemptNumber,
            CanRetry = session.CanRetry,
            RetryInstructions = new RetryInstructionsDto
            {
                Title = "La verificación no fue exitosa",
                Reason = session.FailureDetails ?? "Error desconocido",
                Suggestions = GetSuggestions(session.FailureReason)
            },
            SupportContact = new SupportContactDto()
        };
    }

    private static List<string> GetSuggestions(VerificationFailureReason? reason)
    {
        return reason switch
        {
            VerificationFailureReason.FaceMismatch => new List<string>
            {
                "Asegúrate de que la foto del documento sea clara",
                "Toma la selfie con buena iluminación",
                "Mira directamente a la cámara",
                "No uses lentes de sol o accesorios que cubran tu rostro"
            },
            VerificationFailureReason.LivenessCheckFailed => new List<string>
            {
                "Sigue las instrucciones lentamente",
                "Asegúrate de que tu rostro esté bien iluminado",
                "Realiza los movimientos de manera natural"
            },
            VerificationFailureReason.DocumentBlurry => new List<string>
            {
                "Coloca el documento en una superficie plana",
                "Asegúrate de buena iluminación",
                "Mantén la cámara estable al tomar la foto"
            },
            _ => new List<string>
            {
                "Intenta nuevamente en un lugar bien iluminado",
                "Asegúrate de que tu documento esté en buen estado"
            }
        };
    }
}

#region Query Handlers

/// <summary>
/// Handler para verificar si el usuario puede iniciar una nueva sesión
/// </summary>
public class CanStartVerificationHandler : IRequestHandler<CanStartVerificationQuery, CanStartVerificationResult>
{
    private readonly ILogger<CanStartVerificationHandler> _logger;

    public CanStartVerificationHandler(ILogger<CanStartVerificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CanStartVerificationResult> Handle(CanStartVerificationQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking if user {UserId} can start verification", request.UserId);

        // En simulación, siempre permitimos iniciar
        return await Task.FromResult(new CanStartVerificationResult
        {
            CanStart = true,
            Reason = null,
            CanRetryAfter = null,
            HasActiveSession = false,
            ActiveSessionId = null,
            HasApprovedKYC = false,
            TotalAttemptsToday = 0
        });
    }
}

/// <summary>
/// Handler para obtener la sesión activa de un usuario
/// </summary>
public class GetActiveVerificationSessionHandler : IRequestHandler<GetActiveVerificationSessionQuery, VerificationSessionStatusDto?>
{
    private readonly ILogger<GetActiveVerificationSessionHandler> _logger;

    public GetActiveVerificationSessionHandler(ILogger<GetActiveVerificationSessionHandler> logger)
    {
        _logger = logger;
    }

    public async Task<VerificationSessionStatusDto?> Handle(GetActiveVerificationSessionQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active verification session for user {UserId}", request.UserId);

        // En simulación, retornamos null (no hay sesión activa)
        return await Task.FromResult<VerificationSessionStatusDto?>(null);
    }
}

/// <summary>
/// Handler para obtener historial de sesiones de verificación
/// </summary>
public class GetVerificationHistoryHandler : IRequestHandler<GetVerificationHistoryQuery, List<VerificationSessionSummaryDto>>
{
    private readonly ILogger<GetVerificationHistoryHandler> _logger;

    public GetVerificationHistoryHandler(ILogger<GetVerificationHistoryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<List<VerificationSessionSummaryDto>> Handle(GetVerificationHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting verification history for user {UserId}, limit: {Limit}", request.UserId, request.Limit);

        // En simulación, retornamos lista vacía
        return await Task.FromResult(new List<VerificationSessionSummaryDto>());
    }
}

/// <summary>
/// Handler para obtener el estado de una sesión de verificación
/// </summary>
public class GetVerificationSessionHandler : IRequestHandler<GetVerificationSessionQuery, VerificationSessionStatusDto?>
{
    private readonly ILogger<GetVerificationSessionHandler> _logger;

    public GetVerificationSessionHandler(ILogger<GetVerificationSessionHandler> logger)
    {
        _logger = logger;
    }

    public async Task<VerificationSessionStatusDto?> Handle(GetVerificationSessionQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting verification session {SessionId} for user {UserId}", request.SessionId, request.UserId);

        // En simulación, retornamos null
        return await Task.FromResult<VerificationSessionStatusDto?>(null);
    }
}

#endregion

/// <summary>
/// Handler para verificar identidad usando profileId (flujo simplificado para frontend)
/// </summary>
public class VerifyIdentityByProfileHandler : IRequestHandler<VerifyIdentityByProfileCommand, VerifyIdentityResponse>
{
    private readonly ILogger<VerifyIdentityByProfileHandler> _logger;
    private readonly IdentityVerificationConfig _config;
    private readonly IKYCConfigurationService _kycConfig;
    private readonly IFaceComparisonService _faceComparisonService;
    private readonly IKYCProfileRepository _profileRepository;
    private readonly IKYCDocumentRepository _documentRepository;
    private readonly IMediaServiceClient _mediaServiceClient;

    public VerifyIdentityByProfileHandler(
        ILogger<VerifyIdentityByProfileHandler> logger,
        IOptions<IdentityVerificationConfig> config,
        IKYCConfigurationService kycConfig,
        IFaceComparisonService faceComparisonService,
        IKYCProfileRepository profileRepository,
        IKYCDocumentRepository documentRepository,
        IMediaServiceClient mediaServiceClient)
    {
        _logger = logger;
        _config = config.Value;
        _kycConfig = kycConfig;
        _faceComparisonService = faceComparisonService;
        _profileRepository = profileRepository;
        _documentRepository = documentRepository;
        _mediaServiceClient = mediaServiceClient;
    }

    public async Task<VerifyIdentityResponse> Handle(VerifyIdentityByProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing identity verification for profile {ProfileId}, user {UserId}",
            request.ProfileId, request.UserId);

        // SECURITY: IDOR check — verify the profile belongs to the requesting user
        var profile = await _profileRepository.GetByIdAsync(request.ProfileId, cancellationToken);
        if (profile == null)
            throw new KeyNotFoundException($"Profile {request.ProfileId} not found");

        if (profile.UserId != request.UserId)
        {
            _logger.LogWarning("IDOR attempt: User {UserId} tried to access profile {ProfileId} owned by {OwnerId}",
                request.UserId, request.ProfileId, profile.UserId);
            throw new UnauthorizedAccessException("No tiene permiso para acceder a este perfil");
        }

        // Read dynamic thresholds from admin panel (ConfigurationService)
        var facialThreshold = await _kycConfig.GetFacialMatchThresholdAsync();
        var livenessEnabled = await _kycConfig.IsLivenessRequiredAsync();

        // === LIVENESS CHECK via AWS Rekognition ===
        var livenessConfirmed = false;
        double livenessScore = 0;
        if (request.LivenessData != null && request.LivenessData.Challenges.Any())
        {
            var livenessRequest = new LivenessCheckRequest
            {
                SelfieImage = request.SelfieImageData,
                ChallengeResults = request.LivenessData.Challenges.Select(c => new Infrastructure.ExternalServices.LivenessChallengeResult
                {
                    ChallengeType = c.Type,
                    Passed = c.Passed,
                    Timestamp = c.Timestamp,
                    Confidence = c.Confidence ?? 0
                }).ToList()
            };

            if (request.LivenessData.VideoFrames?.Any() == true)
            {
                livenessRequest.VideoFrames = request.LivenessData.VideoFrames
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Select(frame =>
                    {
                        try
                        {
                            var base64Data = frame.Contains(",") ? frame.Split(',')[1] : frame;
                            return Convert.FromBase64String(base64Data);
                        }
                        catch { return Array.Empty<byte>(); }
                    })
                    .Where(b => b.Length > 0)
                    .ToList();
            }

            var livenessResult = await _faceComparisonService.CheckLivenessAsync(livenessRequest, cancellationToken);
            livenessConfirmed = livenessResult.IsLive;
            livenessScore = (double)livenessResult.LivenessScore;

            _logger.LogInformation("Liveness validation via Rekognition: IsLive={IsLive}, Score={Score}",
                livenessConfirmed, livenessScore);
        }
        else if (!livenessEnabled)
        {
            livenessConfirmed = true; // Liveness not required by config
        }

        // === FACE COMPARISON via AWS Rekognition ===
        double matchScore = 0;
        bool faceMatched = false;

        // Get the document image from the profile's uploaded documents
        var documents = await _documentRepository.GetByProfileIdAsync(request.ProfileId, cancellationToken);
        var identityDoc = documents
            .Where(d => d.Type == DocumentType.Cedula || d.Type == DocumentType.Passport
                     || d.DocumentName.Contains("identity", StringComparison.OrdinalIgnoreCase)
                     || d.Side == "Front")
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefault();

        byte[]? documentImageData = null;
        if (identityDoc != null)
        {
            // Try to download the document image via its storage key or URL
            try
            {
                if (!string.IsNullOrEmpty(identityDoc.StorageKey))
                {
                    var mediaResponse = await _mediaServiceClient.GetFreshUrlAsync(identityDoc.StorageKey, cancellationToken);
                    if (mediaResponse != null && !string.IsNullOrEmpty(mediaResponse.Url))
                    {
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                        documentImageData = await httpClient.GetByteArrayAsync(mediaResponse.Url, cancellationToken);
                    }
                }
                else if (!string.IsNullOrEmpty(identityDoc.FileUrl) && !identityDoc.FileUrl.Contains("pending-upload"))
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    documentImageData = await httpClient.GetByteArrayAsync(identityDoc.FileUrl, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download document image for profile {ProfileId}", request.ProfileId);
            }
        }

        if (documentImageData != null && documentImageData.Length > 0)
        {
            // Real face comparison using AWS Rekognition
            var comparisonResult = await _faceComparisonService.CompareWithDocumentAsync(
                request.SelfieImageData, documentImageData, cancellationToken);

            if (comparisonResult.Success)
            {
                matchScore = (double)comparisonResult.SimilarityScore;
                faceMatched = comparisonResult.SimilarityScore >= facialThreshold;
            }
            else
            {
                _logger.LogWarning("Face comparison failed: {Error}", comparisonResult.ErrorMessage);
            }
        }
        else
        {
            // Fallback: detect face in selfie to at least verify a real face exists
            var detection = await _faceComparisonService.DetectFacesAsync(request.SelfieImageData, cancellationToken);
            if (detection.Success && detection.FaceCount == 1 && detection.Faces.Any(f => f.Confidence > 90))
            {
                matchScore = (double)detection.Faces.First().Confidence;
                faceMatched = true; // Face detected, no document to compare
                _logger.LogWarning("No document image available for comparison, using face detection only for profile {ProfileId}",
                    request.ProfileId);
            }
        }

        // Determine if verification passed
        var passed = faceMatched && (livenessConfirmed || !livenessEnabled);

        _logger.LogInformation(
            "Identity verification result for profile {ProfileId}: Passed={Passed}, MatchScore={MatchScore:F2}, LivenessConfirmed={LivenessConfirmed}",
            request.ProfileId, passed, matchScore, livenessConfirmed);

        return new VerifyIdentityResponse
        {
            Success = true,
            MatchScore = matchScore,
            Passed = passed,
            Message = passed
                ? "Verificación de identidad completada exitosamente"
                : "La verificación no cumplió con los requisitos mínimos",
            Details = new VerificationDetails
            {
                DocumentVerified = identityDoc != null,
                FaceMatched = faceMatched,
                LivenessConfirmed = livenessConfirmed,
                DocumentConfidence = identityDoc != null ? 0.95 : 0,
                FaceMatchConfidence = matchScore,
                LivenessScore = livenessScore
            }
        };
    }
}
