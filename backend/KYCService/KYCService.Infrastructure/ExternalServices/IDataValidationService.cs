namespace KYCService.Infrastructure.ExternalServices;

/// <summary>
/// Interface para validación de datos KYC en República Dominicana.
/// 
/// NOTA IMPORTANTE: En RD no existe una API pública de la JCE para validar cédulas.
/// La verificación KYC se basa en:
/// 1. OCR - Extraer datos de la foto de la cédula
/// 2. Comparación de datos - Verificar que los datos extraídos coinciden con lo registrado
/// 3. Face comparison - Comparar la foto de la cédula con el selfie
/// </summary>
public interface IDataValidationService
{
    /// <summary>
    /// Compara los datos del usuario registrados vs los datos extraídos por OCR
    /// </summary>
    Task<DataComparisonResult> CompareUserDataAsync(
        UserRegistrationData userData,
        OCRExtractedData ocrData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida el formato de la cédula usando el algoritmo Módulo 10
    /// </summary>
    Task<CedulaFormatValidation> ValidateCedulaFormatAsync(
        string cedulaNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si el documento está expirado
    /// </summary>
    Task<DocumentExpirationResult> CheckDocumentExpirationAsync(
        DateTime? expirationDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Datos del usuario proporcionados durante el registro
/// </summary>
public class UserRegistrationData
{
    /// <summary>
    /// Número de cédula proporcionado por el usuario
    /// </summary>
    public string CedulaNumber { get; set; } = string.Empty;

    /// <summary>
    /// Primer nombre
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Segundo nombre (opcional)
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Primer apellido
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Segundo apellido (opcional)
    /// </summary>
    public string? SecondLastName { get; set; }

    /// <summary>
    /// Fecha de nacimiento
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Nombre completo combinado
    /// </summary>
    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName, SecondLastName }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>
/// Datos extraídos del documento por OCR
/// </summary>
public class OCRExtractedData
{
    /// <summary>
    /// Número de cédula extraído del documento
    /// </summary>
    public string? CedulaNumber { get; set; }

    /// <summary>
    /// Nombre completo extraído del documento
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Fecha de nacimiento extraída
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Fecha de expiración del documento
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Nacionalidad
    /// </summary>
    public string? Nationality { get; set; }

    /// <summary>
    /// Confianza del OCR (0-100)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Indica si el documento es legible
    /// </summary>
    public bool IsReadable => Confidence >= 70;
}

/// <summary>
/// Resultado de la comparación de datos
/// </summary>
public class DataComparisonResult
{
    /// <summary>
    /// Indica si todos los datos coinciden
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Indica si la cédula coincide
    /// </summary>
    public bool CedulaMatches { get; set; }

    /// <summary>
    /// Indica si el nombre coincide (usando fuzzy matching)
    /// </summary>
    public bool NameMatches { get; set; }

    /// <summary>
    /// Porcentaje de similitud del nombre (0-100)
    /// </summary>
    public double NameMatchPercentage { get; set; }

    /// <summary>
    /// Indica si la fecha de nacimiento coincide
    /// </summary>
    public bool DateOfBirthMatches { get; set; }

    /// <summary>
    /// Lista de discrepancias encontradas
    /// </summary>
    public List<string> Discrepancies { get; set; } = new();

    /// <summary>
    /// Puntaje general de coincidencia (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// Timestamp de la validación
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Resultado de validación del formato de cédula
/// </summary>
public class CedulaFormatValidation
{
    /// <summary>
    /// Indica si el formato es válido
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Número de cédula limpio (solo dígitos)
    /// </summary>
    public string CleanedNumber { get; set; } = string.Empty;

    /// <summary>
    /// Número de cédula formateado (XXX-XXXXXXX-X)
    /// </summary>
    public string FormattedNumber { get; set; } = string.Empty;

    /// <summary>
    /// Código de municipio
    /// </summary>
    public string? MunicipalityCode { get; set; }

    /// <summary>
    /// Lista de errores de validación
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Resultado de verificación de expiración
/// </summary>
public class DocumentExpirationResult
{
    /// <summary>
    /// Indica si el documento está expirado
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Indica si está próximo a expirar (menos de 30 días)
    /// </summary>
    public bool IsExpiringSoon { get; set; }

    /// <summary>
    /// Días restantes hasta la expiración (negativo si ya expiró)
    /// </summary>
    public int? DaysUntilExpiration { get; set; }

    /// <summary>
    /// Fecha de expiración
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
}
