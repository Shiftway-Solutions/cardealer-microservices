using System.Text.RegularExpressions;

namespace VehiclesSaleService.Application.Services;

/// <summary>
/// VIN (Vehicle Identification Number) validation service.
/// Implements ISO 3779 standard validation including:
/// - Format: 17 alphanumeric characters (excluding I, O, Q)
/// - Check digit: Position 9 checksum verification
/// - WMI: World Manufacturer Identifier extraction
/// </summary>
public static class VinValidationService
{
    private static readonly Regex VinFormatRegex = new(
        @"^[A-HJ-NPR-Z0-9]{17}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Dictionary<char, int> Transliteration = new()
    {
        ['A'] = 1,
        ['B'] = 2,
        ['C'] = 3,
        ['D'] = 4,
        ['E'] = 5,
        ['F'] = 6,
        ['G'] = 7,
        ['H'] = 8,
        ['J'] = 1,
        ['K'] = 2,
        ['L'] = 3,
        ['M'] = 4,
        ['N'] = 5,
        ['P'] = 7,
        ['R'] = 9,
        ['S'] = 2,
        ['T'] = 3,
        ['U'] = 4,
        ['V'] = 5,
        ['W'] = 6,
        ['X'] = 7,
        ['Y'] = 8,
        ['Z'] = 9,
    };

    private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

    /// <summary>
    /// Validates a VIN number for format and checksum correctness.
    /// </summary>
    public static VinValidationResult Validate(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return VinValidationResult.Invalid("VIN is required.");

        var normalized = vin.Trim().ToUpperInvariant();

        if (normalized.Length != 17)
            return VinValidationResult.Invalid($"VIN must be exactly 17 characters (got {normalized.Length}).");

        if (!VinFormatRegex.IsMatch(normalized))
            return VinValidationResult.Invalid("VIN contains invalid characters. Only A-H, J-N, P, R-Z, and 0-9 are allowed (I, O, Q are excluded).");

        if (!ValidateCheckDigit(normalized))
            return VinValidationResult.InvalidChecksum(
                "VIN check digit (position 9) is invalid. This may indicate a fabricated or incorrectly entered VIN.");

        return VinValidationResult.Valid(normalized);
    }

    /// <summary>
    /// Validates only the format (not checksum) — for lenient draft-stage validation.
    /// </summary>
    public static VinValidationResult ValidateFormat(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return VinValidationResult.Valid(null); // VIN optional at draft stage

        var normalized = vin.Trim().ToUpperInvariant();

        if (normalized.Length != 17)
            return VinValidationResult.Invalid($"VIN must be exactly 17 characters (got {normalized.Length}).");

        if (!VinFormatRegex.IsMatch(normalized))
            return VinValidationResult.Invalid("VIN contains invalid characters.");

        return VinValidationResult.Valid(normalized);
    }

    private static bool ValidateCheckDigit(string vin)
    {
        int sum = 0;
        for (int i = 0; i < 17; i++)
        {
            int value;
            if (char.IsDigit(vin[i]))
                value = vin[i] - '0';
            else if (Transliteration.TryGetValue(vin[i], out var mapped))
                value = mapped;
            else
                return false;

            sum += value * Weights[i];
        }

        int remainder = sum % 11;
        char expected = remainder == 10 ? 'X' : (char)('0' + remainder);
        return vin[8] == expected;
    }
}

/// <summary>
/// Result of VIN validation
/// </summary>
public record VinValidationResult
{
    public bool IsValid { get; init; }
    public bool IsChecksumFailure { get; init; }
    public string? NormalizedVin { get; init; }
    public string? Error { get; init; }

    public static VinValidationResult Valid(string? normalizedVin) => new()
    {
        IsValid = true,
        NormalizedVin = normalizedVin
    };

    public static VinValidationResult Invalid(string error) => new()
    {
        IsValid = false,
        Error = error
    };

    public static VinValidationResult InvalidChecksum(string error) => new()
    {
        IsValid = false,
        IsChecksumFailure = true,
        Error = error
    };
}
