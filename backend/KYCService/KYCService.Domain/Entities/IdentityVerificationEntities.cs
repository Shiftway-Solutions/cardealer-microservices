namespace KYCService.Domain.Entities;

/// <summary>
/// Estado de la sesión de verificación de identidad
/// </summary>
public enum VerificationSessionStatus
{
    Started = 1,                // Sesión iniciada
    DocumentFrontCaptured = 2,  // Frente del documento capturado
    DocumentBackCaptured = 3,   // Reverso capturado
    DocumentProcessing = 4,     // Procesando OCR
    AwaitingSelfie = 5,         // Esperando selfie
    SelfieCaptured = 6,         // Selfie capturada
    ProcessingBiometrics = 7,   // Procesando comparación facial
    Completed = 8,              // Verificación completada exitosamente
    Failed = 9,                 // Verificación fallida
    Expired = 10                // Sesión expirada (30 min timeout)
}

/// <summary>
/// Retos de detección de vida (liveness)
/// </summary>
public enum LivenessChallenge
{
    Blink = 1,          // Parpadear
    Smile = 2,          // Sonreír
    TurnLeft = 3,       // Girar cabeza izquierda
    TurnRight = 4,      // Girar cabeza derecha
    Nod = 5,            // Asentir
    OpenMouth = 6       // Abrir boca
}

/// <summary>
/// Lado del documento
/// </summary>
public enum DocumentSide
{
    Front = 1,
    Back = 2
}

/// <summary>
/// Razones de fallo en la verificación
/// </summary>
public enum VerificationFailureReason
{
    None = 0,
    DocumentBlurry = 1,           // Documento borroso
    DocumentCutOff = 2,           // Documento cortado
    DocumentGlare = 3,            // Reflejo en documento
    DocumentExpired = 4,          // Documento expirado
    DocumentFake = 5,             // Documento falso detectado
    FaceNotDetected = 6,          // No se detectó rostro
    FaceMismatch = 7,             // Rostro no coincide
    LivenessCheckFailed = 8,      // Falló detección de vida
    MultipleAttemptsFailed = 9,   // Múltiples intentos fallidos
    SessionExpired = 10,          // Sesión expirada
    OCRFailed = 11,               // Error en extracción OCR
    InvalidDocumentNumber = 12    // Número de documento inválido
}

/// <summary>
/// Sesión de verificación de identidad biométrica
/// Proceso estilo Qik (Banco Popular) para verificación en tiempo real
/// </summary>
public class IdentityVerificationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? KYCProfileId { get; set; }

    // Estado de la sesión
    public VerificationSessionStatus Status { get; set; } = VerificationSessionStatus.Started;
    public VerificationFailureReason? FailureReason { get; set; }
    public string? FailureDetails { get; set; }

    // Documento
    public DocumentType DocumentType { get; set; } = DocumentType.Cedula;
    public string? DocumentFrontUrl { get; set; }
    public string? DocumentBackUrl { get; set; }
    public bool DocumentFrontProcessed { get; set; }
    public bool DocumentBackProcessed { get; set; }

    // Datos OCR extraídos
    public string? ExtractedFullName { get; set; }
    public string? ExtractedFirstName { get; set; }
    public string? ExtractedLastName { get; set; }
    public string? ExtractedDocumentNumber { get; set; }
    public DateTime? ExtractedDateOfBirth { get; set; }
    public DateTime? ExtractedExpiryDate { get; set; }
    public string? ExtractedNationality { get; set; }
    public string? ExtractedGender { get; set; }
    public string? ExtractedAddress { get; set; }
    public decimal OcrConfidence { get; set; } = 0;

    // Selfie y biometría
    public string? SelfieUrl { get; set; }
    public string? LivenessChallengesJson { get; set; }  // JSON array of challenges
    public bool LivenessCheckPassed { get; set; }
    public decimal LivenessScore { get; set; } = 0;  // 0-100

    // Comparación facial
    public decimal FaceMatchScore { get; set; } = 0;  // 0-100
    public bool FaceMatchPassed { get; set; }
    public decimal FaceMatchThreshold { get; set; } = 80.0m;  // 80% mínimo

    // Validación de documento
    public bool DocumentValidationPassed { get; set; }
    public string? DocumentValidationErrorsJson { get; set; }  // JSON array of errors

    // Intentos y límites
    public int AttemptNumber { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;

    // Metadatos del dispositivo
    public string? DeviceInfo { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DocumentFrontCapturedAt { get; set; }
    public DateTime? DocumentBackCapturedAt { get; set; }
    public DateTime? SelfieCapturedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; }  // 30 minutos desde creación

    // Provider de verificación externo (si se usa)
    public string? ExternalProvider { get; set; }  // "Azure", "AWS", "Jumio", "Onfido"
    public string? ExternalSessionId { get; set; }
    public string? ExternalResponseJson { get; set; }  // JSON raw response

    // Navegación
    public KYCProfile? KYCProfile { get; set; }

    /// <summary>
    /// Obtiene los challenges de liveness como lista
    /// </summary>
    public List<LivenessChallenge> GetLivenessChallenges()
    {
        if (string.IsNullOrEmpty(LivenessChallengesJson))
            return new List<LivenessChallenge>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<LivenessChallenge>>(LivenessChallengesJson)
                   ?? new List<LivenessChallenge>();
        }
        catch
        {
            return new List<LivenessChallenge>();
        }
    }

    /// <summary>
    /// Establece los challenges de liveness
    /// </summary>
    public void SetLivenessChallenges(List<LivenessChallenge> challenges)
    {
        LivenessChallengesJson = System.Text.Json.JsonSerializer.Serialize(challenges);
    }

    /// <summary>
    /// Obtiene errores de validación de documento como lista
    /// </summary>
    public List<string> GetDocumentValidationErrors()
    {
        if (string.IsNullOrEmpty(DocumentValidationErrorsJson))
            return new List<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(DocumentValidationErrorsJson)
                   ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Establece errores de validación de documento
    /// </summary>
    public void SetDocumentValidationErrors(List<string> errors)
    {
        DocumentValidationErrorsJson = System.Text.Json.JsonSerializer.Serialize(errors);
    }

    /// <summary>
    /// Verifica si la sesión ha expirado
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Verifica si puede reintentar
    /// </summary>
    public bool CanRetry => AttemptNumber < MaxAttempts && Status == VerificationSessionStatus.Failed;

    /// <summary>
    /// Calcula el score general de la verificación
    /// </summary>
    public decimal CalculateOverallScore()
    {
        if (!DocumentValidationPassed || !LivenessCheckPassed || !FaceMatchPassed)
            return 0;

        // Ponderación: 40% document, 30% liveness, 30% face match
        return (OcrConfidence * 0.4m) + (LivenessScore * 0.3m) + (FaceMatchScore * 0.3m);
    }
}

/// <summary>
/// Resultado de un reto de liveness
/// </summary>
public class LivenessChallengeResult
{
    public LivenessChallenge ChallengeType { get; set; }
    public bool Passed { get; set; }
    public decimal Confidence { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Datos extraídos por OCR de un documento
/// </summary>
public class OcrExtractionResult
{
    public bool Success { get; set; }
    public decimal Confidence { get; set; }

    // Campos extraídos
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? PlaceOfBirth { get; set; }

    // Campos adicionales (para pasaporte)
    public string? MrzLine1 { get; set; }
    public string? MrzLine2 { get; set; }

    // Errores encontrados
    public List<string> Errors { get; set; } = new();

    // Raw data from provider
    public string? RawResponseJson { get; set; }
}

/// <summary>
/// Resultado de validación de documento dominicano
/// </summary>
public class DocumentValidationResult
{
    public bool IsValid { get; set; }
    public bool FormatValid { get; set; }
    public bool ChecksumValid { get; set; }
    public bool NotExpired { get; set; }
    public bool AgeValid { get; set; }  // >= 18 años
    public List<string> Errors { get; set; } = new();
    public string FormattedNumber { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de comparación facial
/// </summary>
public class FaceComparisonResult
{
    public bool Success { get; set; }
    public decimal MatchScore { get; set; }  // 0-100
    public decimal Threshold { get; set; }
    public bool Passed { get; set; }
    public string? ErrorMessage { get; set; }

    // Detalles adicionales
    public bool FaceDetectedInDocument { get; set; }
    public bool FaceDetectedInSelfie { get; set; }
    public decimal DocumentFaceConfidence { get; set; }
    public decimal SelfieFaceConfidence { get; set; }
}

/// <summary>
/// Configuración de verificación de identidad
/// </summary>
public class IdentityVerificationConfig
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Azure";
    public int SessionTimeoutMinutes { get; set; } = 30;
    public int MaxAttempts { get; set; } = 3;
    public int CooldownMinutesAfterMaxAttempts { get; set; } = 1440;  // 24 horas

    public FaceMatchConfig FaceMatch { get; set; } = new();
    public LivenessConfig Liveness { get; set; } = new();
    public DocumentConfig Document { get; set; } = new();
    public CedulaValidationConfig CedulaValidation { get; set; } = new();
}

public class FaceMatchConfig
{
    public decimal MinimumScore { get; set; } = 80.0m;
    public decimal HighConfidenceScore { get; set; } = 95.0m;
}

public class LivenessConfig
{
    public bool Enabled { get; set; } = true;
    public int ChallengesRequired { get; set; } = 3;
    public List<LivenessChallenge> AvailableChallenges { get; set; } = new()
    {
        LivenessChallenge.Blink,
        LivenessChallenge.Smile,
        LivenessChallenge.TurnLeft,
        LivenessChallenge.TurnRight
    };
    public decimal MinimumScore { get; set; } = 70.0m;
}

public class DocumentConfig
{
    public List<DocumentType> AllowedTypes { get; set; } = new()
    {
        DocumentType.Cedula,
        DocumentType.Passport
    };
    public bool RequireBothSides { get; set; } = true;
    public decimal OcrConfidenceThreshold { get; set; } = 0.8m;
    public int MaxFileSizeMB { get; set; } = 10;
    public int ExpirationDays { get; set; } = 365;
    public List<string> AllowedFormats { get; set; } = new() { "jpg", "jpeg", "png", "heic" };
}

public class CedulaValidationConfig
{
    public bool ValidateChecksum { get; set; } = true;
    public bool ValidateFormat { get; set; } = true;
    public bool ValidateExpiry { get; set; } = true;
    public int MinimumAge { get; set; } = 18;
    public bool JCEIntegrationEnabled { get; set; } = false;
}
