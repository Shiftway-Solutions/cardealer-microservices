using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KYCService.Domain.Validators;

namespace KYCService.Infrastructure.ExternalServices;

/// <summary>
/// Servicio de validación de datos KYC para República Dominicana.
/// 
/// Este servicio NO usa API de JCE (no existe API pública).
/// En cambio, compara:
/// 1. Datos extraídos por OCR del documento
/// 2. Datos proporcionados por el usuario durante el registro
/// 3. Foto del documento vs selfie (vía FaceComparisonService)
/// </summary>
public class DataValidationService : IDataValidationService
{
    private readonly ILogger<DataValidationService> _logger;
    private readonly DataValidationConfig _config;

    public DataValidationService(
        ILogger<DataValidationService> logger,
        IOptions<DataValidationConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <inheritdoc />
    public Task<DataComparisonResult> CompareUserDataAsync(
        UserRegistrationData userData,
        OCRExtractedData ocrData,
        CancellationToken cancellationToken = default)
    {
        var result = new DataComparisonResult();

        _logger.LogInformation("Comparing user data with OCR extracted data for cedula {CedulaMasked}",
            MaskCedula(userData.CedulaNumber));

        // 1. Comparar número de cédula
        var cedulaComparison = CompareCedulas(userData.CedulaNumber, ocrData.CedulaNumber);
        result.CedulaMatches = cedulaComparison;
        if (!cedulaComparison)
        {
            result.Discrepancies.Add("El número de cédula no coincide con el documento");
        }

        // 2. Comparar nombres (con fuzzy matching)
        var nameComparison = CompareNames(userData.FullName, ocrData.FullName);
        result.NameMatches = nameComparison.isMatch;
        result.NameMatchPercentage = nameComparison.percentage;
        if (!nameComparison.isMatch)
        {
            result.Discrepancies.Add($"El nombre no coincide (similitud: {nameComparison.percentage:F0}%)");
        }

        // 3. Comparar fecha de nacimiento (si disponible)
        if (userData.DateOfBirth.HasValue && ocrData.DateOfBirth.HasValue)
        {
            result.DateOfBirthMatches = userData.DateOfBirth.Value.Date == ocrData.DateOfBirth.Value.Date;
            if (!result.DateOfBirthMatches)
            {
                result.Discrepancies.Add("La fecha de nacimiento no coincide");
            }
        }
        else
        {
            // Si no hay fecha para comparar, lo consideramos como match (opcional)
            result.DateOfBirthMatches = true;
        }

        // Calcular puntaje general
        result.OverallScore = CalculateOverallScore(result);
        result.IsMatch = result.OverallScore >= _config.MinimumMatchScore;

        _logger.LogInformation(
            "Data comparison complete for {CedulaMasked}: Match={IsMatch}, Score={Score:F0}%",
            MaskCedula(userData.CedulaNumber), result.IsMatch, result.OverallScore);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<CedulaFormatValidation> ValidateCedulaFormatAsync(
        string cedulaNumber,
        CancellationToken cancellationToken = default)
    {
        var detailedResult = CedulaValidator.ValidateDetailed(cedulaNumber);

        var result = new CedulaFormatValidation
        {
            IsValid = detailedResult.IsValid,
            CleanedNumber = detailedResult.CleanedNumber,
            FormattedNumber = CedulaValidator.FormatCedula(detailedResult.CleanedNumber),
            MunicipalityCode = detailedResult.CleanedNumber.Length >= 3
                ? detailedResult.CleanedNumber[..3]
                : null,
            Errors = detailedResult.Errors.ToList()
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<DocumentExpirationResult> CheckDocumentExpirationAsync(
        DateTime? expirationDate,
        CancellationToken cancellationToken = default)
    {
        var result = new DocumentExpirationResult
        {
            ExpirationDate = expirationDate
        };

        if (expirationDate.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            var expDate = expirationDate.Value.Date;
            var daysUntil = (expDate - today).Days;

            result.DaysUntilExpiration = daysUntil;
            result.IsExpired = daysUntil < 0;
            result.IsExpiringSoon = daysUntil >= 0 && daysUntil <= 30;
        }

        return Task.FromResult(result);
    }

    #region Private Methods

    /// <summary>
    /// Compara dos números de cédula (normalizados)
    /// </summary>
    private static bool CompareCedulas(string? cedula1, string? cedula2)
    {
        if (string.IsNullOrWhiteSpace(cedula1) || string.IsNullOrWhiteSpace(cedula2))
            return false;

        var clean1 = CedulaValidator.CleanCedula(cedula1);
        var clean2 = CedulaValidator.CleanCedula(cedula2);

        return clean1 == clean2;
    }

    /// <summary>
    /// Compara dos nombres usando fuzzy matching (Levenshtein distance)
    /// </summary>
    private (bool isMatch, double percentage) CompareNames(string? name1, string? name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            return (false, 0);

        // Normalizar nombres
        var normalized1 = NormalizeName(name1);
        var normalized2 = NormalizeName(name2);

        // Calcular similitud
        var similarity = CalculateNameSimilarity(normalized1, normalized2);
        var isMatch = similarity >= _config.NameMatchThreshold;

        return (isMatch, similarity);
    }

    /// <summary>
    /// Normaliza un nombre para comparación
    /// </summary>
    private static string NormalizeName(string name)
    {
        return name
            .ToUpperInvariant()
            .Replace("Á", "A")
            .Replace("É", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ú", "U")
            .Replace("Ñ", "N")
            .Replace("Ü", "U")
            .Replace(".", "")
            .Replace(",", "")
            .Trim();
    }

    /// <summary>
    /// Calcula similitud entre dos nombres usando múltiples estrategias
    /// </summary>
    private double CalculateNameSimilarity(string name1, string name2)
    {
        // Estrategia 1: Coincidencia exacta
        if (name1 == name2)
            return 100;

        // Estrategia 2: Coincidencia de palabras
        var words1 = name1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = name2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        if (union == 0) return 0;

        var jaccardSimilarity = (double)intersection / union * 100;

        // Estrategia 3: Levenshtein distance (para nombres similares con typos)
        var levenshteinSimilarity = CalculateLevenshteinSimilarity(name1, name2);

        // Usar el mayor de los dos scores
        return Math.Max(jaccardSimilarity, levenshteinSimilarity);
    }

    /// <summary>
    /// Calcula similitud basada en Levenshtein distance
    /// </summary>
    private static double CalculateLevenshteinSimilarity(string s1, string s2)
    {
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        if (maxLength == 0) return 100;

        return (1 - (double)distance / maxLength) * 100;
    }

    /// <summary>
    /// Implementación de Levenshtein distance
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s2[j - 1] == s1[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Calcula el puntaje general de coincidencia
    /// </summary>
    private double CalculateOverallScore(DataComparisonResult result)
    {
        double score = 0;
        double totalWeight = 0;

        // Cédula: peso alto (40%)
        if (result.CedulaMatches)
        {
            score += 40;
        }
        totalWeight += 40;

        // Nombre: peso medio-alto (40%)
        score += (result.NameMatchPercentage / 100) * 40;
        totalWeight += 40;

        // Fecha de nacimiento: peso medio (20%)
        if (result.DateOfBirthMatches)
        {
            score += 20;
        }
        totalWeight += 20;

        return (score / totalWeight) * 100;
    }

    /// <summary>
    /// Enmascara una cédula para logs (XXX-XXXX***-*)
    /// </summary>
    private static string MaskCedula(string? cedula)
    {
        if (string.IsNullOrEmpty(cedula) || cedula.Length < 5)
            return "***";

        var clean = CedulaValidator.CleanCedula(cedula);
        if (clean.Length < 5) return "***";

        return $"{clean[..3]}-{clean.Substring(3, 4)}***-*";
    }

    #endregion
}

/// <summary>
/// Configuración del servicio de validación de datos
/// </summary>
public class DataValidationConfig
{
    /// <summary>
    /// Umbral mínimo para considerar que los nombres coinciden (0-100)
    /// </summary>
    public double NameMatchThreshold { get; set; } = 85;

    /// <summary>
    /// Puntaje mínimo general para considerar la validación exitosa (0-100)
    /// </summary>
    public double MinimumMatchScore { get; set; } = 80;

    /// <summary>
    /// Permitir coincidencias parciales de nombre
    /// </summary>
    public bool AllowFuzzyNameMatch { get; set; } = true;
}
