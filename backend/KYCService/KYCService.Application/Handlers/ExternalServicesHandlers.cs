using MediatR;
using Microsoft.Extensions.Logging;
using KYCService.Application.Commands;
using KYCService.Application.DTOs;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Domain.Validators;
using KYCService.Infrastructure.ExternalServices;

namespace KYCService.Application.Handlers;

/// <summary>
/// Handler para verificar identidad contra JCE
/// Usa IJCEService para validar cédula con la Junta Central Electoral
/// </summary>
public class VerifyIdentityWithJCEHandler : IRequestHandler<VerifyIdentityWithJCECommand, JCEVerificationResultDto>
{
    private readonly ILogger<VerifyIdentityWithJCEHandler> _logger;
    private readonly IJCEService _jceService;
    private readonly IKYCProfileRepository _profileRepository;

    public VerifyIdentityWithJCEHandler(
        ILogger<VerifyIdentityWithJCEHandler> logger,
        IJCEService jceService,
        IKYCProfileRepository profileRepository)
    {
        _logger = logger;
        _jceService = jceService;
        _profileRepository = profileRepository;
    }

    public async Task<JCEVerificationResultDto> Handle(
        VerifyIdentityWithJCECommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying identity with JCE for cedula {Cedula}",
            MaskCedula(request.CedulaNumber));

        try
        {
            // 1. Validar formato de cédula primero (local)
            var localValidation = CedulaValidator.ValidateDetailed(request.CedulaNumber);
            if (!localValidation.IsValid)
            {
                _logger.LogWarning("Cedula failed local validation: {Errors}",
                    string.Join(", ", localValidation.Errors));

                return new JCEVerificationResultDto
                {
                    Success = false,
                    IsValid = false,
                    ValidationErrors = localValidation.Errors,
                    Source = "LOCAL"
                };
            }

            // 2. Validar con JCE
            var jceResult = await _jceService.ValidateCedulaAsync(request.CedulaNumber, cancellationToken);

            if (!jceResult.IsValid)
            {
                return new JCEVerificationResultDto
                {
                    Success = false,
                    IsValid = false,
                    JCEStatus = jceResult.IsActive ? "INACTIVE" : "INVALID",
                    ValidationErrors = new List<string> { jceResult.ErrorMessage ?? "Cédula no válida en JCE" },
                    Source = "JCE"
                };
            }

            // 3. Obtener datos ciudadanos de JCE
            var citizenData = await _jceService.GetCitizenDataAsync(request.CedulaNumber, cancellationToken);

            if (citizenData == null)
            {
                return new JCEVerificationResultDto
                {
                    Success = false,
                    IsValid = true, // Cédula válida pero sin datos
                    JCEStatus = "NO_DATA",
                    ValidationErrors = new List<string> { "No se pudieron obtener datos del ciudadano" },
                    Source = "JCE"
                };
            }

            // 4. Verificar que datos coinciden con lo proporcionado
            var nameMatch = CompareNames(
                request.ProvidedFullName,
                $"{citizenData.FirstName} {citizenData.LastName}");

            var dobMatch = request.ProvidedDateOfBirth.HasValue &&
                           citizenData.DateOfBirth.Date == request.ProvidedDateOfBirth.Value.Date;

            // 5. Construir respuesta exitosa
            var result = new JCEVerificationResultDto
            {
                Success = true,
                IsValid = true,
                JCEStatus = "VERIFIED",
                Source = "JCE",
                VerifiedData = new JCEVerifiedDataDto
                {
                    FullName = $"{citizenData.FirstName} {citizenData.LastName}",
                    FirstName = citizenData.FirstName,
                    LastName = citizenData.LastName,
                    DateOfBirth = citizenData.DateOfBirth,
                    BirthPlace = citizenData.BirthPlace,
                    Nationality = citizenData.Nationality,
                    Gender = citizenData.Gender,
                    PhotoUrl = citizenData.PhotoUrl,
                    MunicipalityCode = citizenData.Municipality
                },
                MatchResults = new JCEMatchResultsDto
                {
                    NameMatch = nameMatch,
                    DateOfBirthMatch = dobMatch,
                    OverallMatch = nameMatch && dobMatch
                },
                VerifiedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "JCE verification completed for cedula {Cedula}: Valid={IsValid}, NameMatch={NameMatch}",
                MaskCedula(request.CedulaNumber), result.IsValid, nameMatch);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during JCE verification for cedula {Cedula}",
                MaskCedula(request.CedulaNumber));

            return new JCEVerificationResultDto
            {
                Success = false,
                IsValid = false,
                ValidationErrors = new List<string> { "Error al conectar con servicio de verificación" },
                Source = "ERROR"
            };
        }
    }

    private string MaskCedula(string cedula)
    {
        if (string.IsNullOrEmpty(cedula) || cedula.Length < 6)
            return "***";
        return $"{cedula[..3]}****{cedula[^4..]}";
    }

    private bool CompareNames(string? provided, string jceFullName)
    {
        if (string.IsNullOrEmpty(provided))
            return false;

        // Normalizar nombres (quitar acentos, mayúsculas, espacios extra)
        var normalizedProvided = NormalizeName(provided);
        var normalizedJce = NormalizeName(jceFullName);

        // Comparar exacto primero
        if (normalizedProvided == normalizedJce)
            return true;

        // Comparar palabras (orden puede variar)
        var providedWords = normalizedProvided.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var jceWords = normalizedJce.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        // Al menos 80% de las palabras deben coincidir
        var matchingWords = providedWords.Intersect(jceWords).Count();
        var totalWords = Math.Max(providedWords.Count, jceWords.Count);

        return (double)matchingWords / totalWords >= 0.8;
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        return name
            .ToUpperInvariant()
            .Replace("Á", "A")
            .Replace("É", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ú", "U")
            .Replace("Ñ", "N")
            .Trim();
    }
}

/// <summary>
/// Handler para procesar OCR de documentos
/// </summary>
public class ProcessDocumentOCRHandler : IRequestHandler<ProcessDocumentOCRCommand, OCRProcessingResultDto>
{
    private readonly ILogger<ProcessDocumentOCRHandler> _logger;
    private readonly IOCRService _ocrService;

    public ProcessDocumentOCRHandler(
        ILogger<ProcessDocumentOCRHandler> logger,
        IOCRService ocrService)
    {
        _logger = logger;
        _ocrService = ocrService;
    }

    public async Task<OCRProcessingResultDto> Handle(
        ProcessDocumentOCRCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing OCR for document side {Side}, session {SessionId}",
            request.Side, request.SessionId);

        try
        {
            // 1. Verificar calidad de imagen primero
            var qualityResult = await _ocrService.CheckImageQualityAsync(
                request.ImageData, cancellationToken);

            if (!qualityResult.IsAcceptable)
            {
                return new OCRProcessingResultDto
                {
                    Success = false,
                    Error = "La calidad de la imagen no es aceptable",
                    QualityIssues = qualityResult.Issues,
                    Suggestions = qualityResult.Suggestions
                };
            }

            // 2. Procesar OCR según el lado del documento
            CedulaOCRResult ocrResult;

            if (request.Side == DocumentSide.Front)
            {
                ocrResult = await _ocrService.ExtractCedulaFrontAsync(
                    request.ImageData, cancellationToken);
            }
            else
            {
                ocrResult = await _ocrService.ExtractCedulaBackAsync(
                    request.ImageData, cancellationToken);
            }

            // 3. Validar datos extraídos
            var extractedData = new ExtractedDocumentDataDto
            {
                CedulaNumber = ocrResult.CedulaNumber,
                FullName = ocrResult.FullName,
                FirstName = ocrResult.FirstName,
                LastName = ocrResult.LastName,
                DateOfBirth = ocrResult.DateOfBirth,
                ExpiryDate = ocrResult.ExpiryDate,
                Nationality = ocrResult.Nationality,
                Address = ocrResult.Address,
                BloodType = null, // No está en CedulaOCRResult
                MaritalStatus = ocrResult.MaritalStatus
            };

            // 4. Validar cédula si se extrajo
            List<string>? cedulaValidationErrors = null;
            if (!string.IsNullOrEmpty(ocrResult.CedulaNumber))
            {
                var validation = CedulaValidator.ValidateDetailed(ocrResult.CedulaNumber);
                if (!validation.IsValid)
                {
                    cedulaValidationErrors = validation.Errors;
                }
            }

            _logger.LogInformation(
                "OCR completed for session {SessionId}: Confidence={Confidence}, CedulaExtracted={HasCedula}",
                request.SessionId, ocrResult.Confidence, !string.IsNullOrEmpty(ocrResult.CedulaNumber));

            return new OCRProcessingResultDto
            {
                Success = ocrResult.Success,
                Confidence = ocrResult.Confidence,
                ExtractedData = extractedData,
                CedulaValidationErrors = cedulaValidationErrors,
                ImageQuality = new ImageQualityDto
                {
                    IsAcceptable = qualityResult.IsAcceptable,
                    Brightness = qualityResult.Metrics.Brightness,
                    Contrast = qualityResult.Metrics.Contrast,
                    Sharpness = qualityResult.Metrics.Sharpness,
                    Resolution = $"{qualityResult.Metrics.Width}x{qualityResult.Metrics.Height}"
                },
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OCR for session {SessionId}", request.SessionId);

            return new OCRProcessingResultDto
            {
                Success = false,
                Error = "Error interno al procesar el documento"
            };
        }
    }
}

/// <summary>
/// Handler para comparación facial
/// </summary>
public class CompareFacesHandler : IRequestHandler<CompareFacesCommand, FaceComparisonResultDto>
{
    private readonly ILogger<CompareFacesHandler> _logger;
    private readonly IFaceComparisonService _faceService;

    public CompareFacesHandler(
        ILogger<CompareFacesHandler> logger,
        IFaceComparisonService faceService)
    {
        _logger = logger;
        _faceService = faceService;
    }

    public async Task<FaceComparisonResultDto> Handle(
        CompareFacesCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Comparing faces for session {SessionId}", request.SessionId);

        try
        {
            // 1. Detectar rostro en documento
            var documentFaceResult = await _faceService.DetectFacesAsync(
                request.DocumentImage, cancellationToken);

            if (!documentFaceResult.Success || documentFaceResult.FaceCount == 0)
            {
                return new FaceComparisonResultDto
                {
                    Success = false,
                    FaceDetectedInDocument = false,
                    Error = "No se detectó un rostro en el documento"
                };
            }

            if (documentFaceResult.FaceCount > 1)
            {
                return new FaceComparisonResultDto
                {
                    Success = false,
                    FaceDetectedInDocument = true,
                    Error = "Se detectaron múltiples rostros en el documento"
                };
            }

            // 2. Detectar rostro en selfie
            var selfieFaceResult = await _faceService.DetectFacesAsync(
                request.SelfieImage, cancellationToken);

            if (!selfieFaceResult.Success || selfieFaceResult.FaceCount == 0)
            {
                return new FaceComparisonResultDto
                {
                    Success = false,
                    FaceDetectedInDocument = true,
                    FaceDetectedInSelfie = false,
                    Error = "No se detectó un rostro en la selfie"
                };
            }

            // 3. Comparar rostros
            var comparisonResult = await _faceService.CompareFacesAsync(
                request.DocumentImage,
                request.SelfieImage,
                cancellationToken);

            // 4. Procesar liveness si se proporcionaron datos
            LivenessResultDto? livenessResult = null;
            if (request.LivenessFrames != null && request.LivenessFrames.Any())
            {
                var livenessRequest = new LivenessCheckRequest
                {
                    SelfieImage = request.SelfieImage,
                    VideoFrames = request.LivenessFrames,
                    ChallengeResults = new List<Infrastructure.ExternalServices.LivenessChallengeResult>
                    {
                        new Infrastructure.ExternalServices.LivenessChallengeResult
                        {
                            ChallengeType = request.ChallengeType ?? "BLINK",
                            Passed = true,
                            Timestamp = DateTime.UtcNow,
                            Confidence = 90
                        }
                    }
                };

                var liveness = await _faceService.CheckLivenessAsync(livenessRequest, cancellationToken);

                livenessResult = new LivenessResultDto
                {
                    Passed = liveness.IsLive,
                    Confidence = liveness.LivenessScore,
                    ChallengesCompleted = liveness.ChallengeDetails
                        .Where(c => c.Passed)
                        .Select(c => c.ChallengeType)
                        .ToList()
                };
            }

            _logger.LogInformation(
                "Face comparison completed for session {SessionId}: Match={IsMatch}, Score={Score}",
                request.SessionId, comparisonResult.IsMatch, comparisonResult.SimilarityScore);

            return new FaceComparisonResultDto
            {
                Success = true,
                FaceDetectedInDocument = true,
                FaceDetectedInSelfie = true,
                IsMatch = comparisonResult.IsMatch,
                MatchScore = comparisonResult.SimilarityScore,
                Confidence = comparisonResult.Confidence,
                Liveness = livenessResult,
                DocumentFaceDetails = documentFaceResult.Faces.Count > 0 ? new FaceDetailsDto
                {
                    Age = documentFaceResult.Faces[0].EstimatedAge,
                    Gender = documentFaceResult.Faces[0].DetectedGender,
                    Glasses = null // No está disponible en DetectedFace
                } : null,
                SelfieFaceDetails = selfieFaceResult.Faces.Count > 0 ? new FaceDetailsDto
                {
                    Age = selfieFaceResult.Faces[0].EstimatedAge,
                    Gender = selfieFaceResult.Faces[0].DetectedGender,
                    Glasses = null
                } : null,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing faces for session {SessionId}", request.SessionId);

            return new FaceComparisonResultDto
            {
                Success = false,
                Error = "Error interno al comparar rostros"
            };
        }
    }
}
