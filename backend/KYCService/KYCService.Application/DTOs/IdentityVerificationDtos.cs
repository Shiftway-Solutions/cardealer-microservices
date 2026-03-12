using KYCService.Domain.Entities;

namespace KYCService.Application.DTOs;

/// <summary>
/// Request para iniciar sesión de verificación de identidad
/// </summary>
public class StartVerificationRequest
{
    public DocumentType DocumentType { get; set; } = DocumentType.Cedula;
    public DeviceInfoDto? DeviceInfo { get; set; }
    public LocationDto? Location { get; set; }
}

/// <summary>
/// Información del dispositivo
/// </summary>
public class DeviceInfoDto
{
    public string? Platform { get; set; }        // iOS, Android, Web
    public string? Version { get; set; }          // OS version
    public string? Model { get; set; }            // Device model
    public string? AppVersion { get; set; }       // App version
    public string? Browser { get; set; }          // Browser name (for web)
    public string? UserAgent { get; set; }
}

/// <summary>
/// Ubicación del dispositivo
/// </summary>
public class LocationDto
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

/// <summary>
/// Response al iniciar sesión de verificación
/// </summary>
public class StartVerificationResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = "Started";
    public string DocumentType { get; set; } = "Cedula";
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string NextStep { get; set; } = "CAPTURE_DOCUMENT_FRONT";
    public VerificationInstructionsDto Instructions { get; set; } = new();
    public List<string> RequiredChallenges { get; set; } = new();
}

/// <summary>
/// Instrucciones para el usuario
/// </summary>
public class VerificationInstructionsDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public List<string> Tips { get; set; } = new();
}

/// <summary>
/// Response después de procesar documento
/// </summary>
public class DocumentProcessedResponse
{
    public Guid SessionId { get; set; }
    public string Side { get; set; } = "Front";
    public string Status { get; set; } = string.Empty;
    public OcrResultDto? OcrResult { get; set; }
    public DocumentValidationDto? DocumentValidation { get; set; }
    public string NextStep { get; set; } = string.Empty;
    public VerificationInstructionsDto? Instructions { get; set; }
}

/// <summary>
/// Resultado de OCR
/// </summary>
public class OcrResultDto
{
    public bool Success { get; set; }
    public OcrExtractedDataDto? ExtractedData { get; set; }
    public decimal Confidence { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Datos extraídos por OCR
/// </summary>
public class OcrExtractedDataDto
{
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DateOfBirth { get; set; }       // ISO format
    public string? ExpiryDate { get; set; }        // ISO format
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Resultado de validación de documento
/// </summary>
public class DocumentValidationDto
{
    public bool FormatValid { get; set; }
    public bool ChecksumValid { get; set; }
    public bool NotExpired { get; set; }
    public bool AgeValid { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Request para subir selfie con liveness data
/// </summary>
public class SelfieUploadRequest
{
    public Guid SessionId { get; set; }
    // SelfieImage viene como IFormFile en el controller
    public LivenessDataDto? LivenessData { get; set; }
}

/// <summary>
/// Datos de liveness detection
/// </summary>
public class LivenessDataDto
{
    public List<ChallengeResultDto> Challenges { get; set; } = new();
    public List<string>? VideoFrames { get; set; }       // Base64 encoded frames
    public string? DeviceGyroscope { get; set; }          // JSON gyroscope data
}

/// <summary>
/// Resultado de un challenge de liveness
/// </summary>
public class ChallengeResultDto
{
    public string Type { get; set; } = string.Empty;      // Blink, Smile, TurnLeft, etc.
    public bool Passed { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? Confidence { get; set; }
}

/// <summary>
/// Response de verificación completada exitosamente
/// </summary>
public class VerificationCompletedResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = "Completed";
    public VerificationResultDto Result { get; set; } = new();
    public ExtractedProfileDto? ExtractedProfile { get; set; }
    public Guid? KYCProfileId { get; set; }
    public string KYCStatus { get; set; } = "PendingReview";
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Resultado detallado de la verificación
/// </summary>
public class VerificationResultDto
{
    public bool Verified { get; set; }
    public decimal OverallScore { get; set; }
    public VerificationDetailsDto Details { get; set; } = new();
}

/// <summary>
/// Detalles de cada verificación
/// </summary>
public class VerificationDetailsDto
{
    public DocumentAuthenticityDto DocumentAuthenticity { get; set; } = new();
    public LivenessDetectionDto LivenessDetection { get; set; } = new();
    public FaceMatchDto FaceMatch { get; set; } = new();
    public OcrAccuracyDto OcrAccuracy { get; set; } = new();
}

public class DocumentAuthenticityDto
{
    public bool Passed { get; set; }
    public decimal Score { get; set; }
    public List<string> Checks { get; set; } = new() { "format", "checksum", "expiry", "tampering" };
}

public class LivenessDetectionDto
{
    public bool Passed { get; set; }
    public decimal Score { get; set; }
    public int ChallengesPassed { get; set; }
    public int ChallengesTotal { get; set; }
}

public class FaceMatchDto
{
    public bool Passed { get; set; }
    public decimal Score { get; set; }
    public decimal Threshold { get; set; }
}

public class OcrAccuracyDto
{
    public decimal Confidence { get; set; }
    public int FieldsExtracted { get; set; }
    public int FieldsTotal { get; set; }
}

/// <summary>
/// Perfil extraído de la verificación
/// </summary>
public class ExtractedProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Cedula";
    public string? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Response de verificación fallida
/// </summary>
public class VerificationFailedResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = "Failed";
    public FailedResultDto Result { get; set; } = new();
    public int AttemptsRemaining { get; set; }
    public bool CanRetry { get; set; }
    public RetryInstructionsDto? RetryInstructions { get; set; }
    public SupportContactDto? SupportContact { get; set; }
}

public class FailedResultDto
{
    public bool Verified { get; set; } = false;
    public string FailureReason { get; set; } = string.Empty;
    public string FailureDetails { get; set; } = string.Empty;
    public Dictionary<string, ScoreDto> Scores { get; set; } = new();
}

public class ScoreDto
{
    public decimal Score { get; set; }
    public decimal? Threshold { get; set; }
    public bool Passed { get; set; }
}

public class RetryInstructionsDto
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
}

public class SupportContactDto
{
    public string Email { get; set; } = "soporte@okla.com.do";
    public string Phone { get; set; } = "+1 809-555-0000";
    public string WhatsApp { get; set; } = "+1 809-555-0001";
}

/// <summary>
/// Estado de sesión de verificación
/// </summary>
public class VerificationSessionStatusDto
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public int AttemptNumber { get; set; }
    public int MaxAttempts { get; set; }
    public bool CanRetry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public string CurrentStep { get; set; } = string.Empty;

    // Progreso
    public bool DocumentFrontCaptured { get; set; }
    public bool DocumentBackCaptured { get; set; }
    public bool SelfieCapture { get; set; }
    public bool LivenessCompleted { get; set; }

    // Scores (si completado)
    public decimal? LivenessScore { get; set; }
    public decimal? FaceMatchScore { get; set; }
    public decimal? OcrConfidence { get; set; }
    public decimal? OverallScore { get; set; }
}

/// <summary>
/// DTO resumido para listados
/// </summary>
public class VerificationSessionSummaryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? OverallScore { get; set; }
    public bool Verified { get; set; }
}
