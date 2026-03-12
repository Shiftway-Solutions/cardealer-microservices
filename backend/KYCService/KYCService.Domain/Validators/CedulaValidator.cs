namespace KYCService.Domain.Validators;

/// <summary>
/// Validador de cédula dominicana
/// Implementa la validación según formato de JCE (Junta Central Electoral)
/// Formato: XXX-XXXXXXX-X (001-0000000-0)
/// </summary>
public static class CedulaValidator
{
    /// <summary>
    /// Códigos de municipio válidos en República Dominicana (001-044)
    /// </summary>
    private static readonly HashSet<int> ValidMunicipios = new()
    {
        1,   // Distrito Nacional
        2,   // Azua
        3,   // Bahoruco
        4,   // Barahona
        5,   // Dajabón
        6,   // Duarte
        7,   // Elías Piña
        8,   // El Seibo
        9,   // Espaillat
        10,  // Independencia
        11,  // La Altagracia
        12,  // La Romana
        13,  // La Vega
        14,  // María Trinidad Sánchez
        15,  // Monte Cristi
        16,  // Pedernales
        17,  // Peravia
        18,  // Puerto Plata
        19,  // Hermanas Mirabal (antes Salcedo)
        20,  // Samaná
        21,  // San Cristóbal
        22,  // San Juan
        23,  // San Pedro de Macorís
        24,  // Sánchez Ramírez
        25,  // Santiago
        26,  // Santiago Rodríguez
        27,  // Valverde
        28,  // Monseñor Nouel
        29,  // Monte Plata
        30,  // Hato Mayor
        31,  // San José de Ocoa
        32,  // Santo Domingo
        33,  // La Romana (extensión)
        34,  // Santiago (extensión)
        35,  // Distrito Nacional (extensión)
        36,  // Santo Domingo Norte
        37,  // Santo Domingo Este
        38,  // Santo Domingo Oeste
        39,  // Santo Domingo (extensión)
        40,  // Santiago de los Caballeros
        41,  // Extensión
        42,  // Extensión
        43,  // Extensión
        44   // Extensión
    };

    /// <summary>
    /// Valida el formato y dígito verificador de una cédula dominicana
    /// </summary>
    /// <param name="cedula">Número de cédula (con o sin guiones)</param>
    /// <returns>Tupla con resultado de validación y mensaje de error si aplica</returns>
    public static (bool IsValid, string? Error) ValidateCedula(string cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula))
            return (false, "La cédula es requerida");

        // Remover guiones, espacios y caracteres especiales
        var cleaned = CleanCedula(cedula);

        // Validar longitud
        if (cleaned.Length != 11)
            return (false, "La cédula debe tener 11 dígitos");

        // Validar que solo contenga números
        if (!cleaned.All(char.IsDigit))
            return (false, "La cédula solo debe contener números");

        // Validar municipio (primeros 3 dígitos)
        var municipio = int.Parse(cleaned.Substring(0, 3));
        if (!ValidMunicipios.Contains(municipio))
            return (false, $"Código de municipio inválido: {municipio:D3}");

        // Calcular y validar dígito verificador
        var expectedCheckDigit = CalculateCheckDigit(cleaned.Substring(0, 10));
        var actualCheckDigit = int.Parse(cleaned[10].ToString());

        if (expectedCheckDigit != actualCheckDigit)
            return (false, "Dígito verificador inválido");

        return (true, null);
    }

    /// <summary>
    /// Valida una cédula y retorna un resultado detallado
    /// </summary>
    public static CedulaValidationResult ValidateDetailed(string cedula)
    {
        var result = new CedulaValidationResult();

        if (string.IsNullOrWhiteSpace(cedula))
        {
            result.Errors.Add("La cédula es requerida");
            return result;
        }

        var cleaned = CleanCedula(cedula);
        result.CleanedNumber = cleaned;
        result.FormattedNumber = FormatCedula(cleaned);

        // Validar longitud
        if (cleaned.Length != 11)
        {
            result.Errors.Add($"La cédula debe tener 11 dígitos (tiene {cleaned.Length})");
            return result;
        }

        // Validar solo números
        if (!cleaned.All(char.IsDigit))
        {
            result.Errors.Add("La cédula solo debe contener números");
            return result;
        }

        // Validar formato
        result.FormatValid = true;

        // Validar municipio
        var municipio = int.Parse(cleaned.Substring(0, 3));
        result.Municipio = municipio;
        if (!ValidMunicipios.Contains(municipio))
        {
            result.Errors.Add($"Código de municipio inválido: {municipio:D3}");
            result.MunicipioValid = false;
        }
        else
        {
            result.MunicipioValid = true;
        }

        // Calcular dígito verificador
        var expectedCheckDigit = CalculateCheckDigit(cleaned.Substring(0, 10));
        var actualCheckDigit = int.Parse(cleaned[10].ToString());
        result.ExpectedCheckDigit = expectedCheckDigit;
        result.ActualCheckDigit = actualCheckDigit;

        if (expectedCheckDigit != actualCheckDigit)
        {
            result.Errors.Add($"Dígito verificador inválido (esperado: {expectedCheckDigit}, recibido: {actualCheckDigit})");
            result.ChecksumValid = false;
        }
        else
        {
            result.ChecksumValid = true;
        }

        // Resultado final
        result.IsValid = result.FormatValid && result.MunicipioValid && result.ChecksumValid;

        return result;
    }

    /// <summary>
    /// Calcula el dígito verificador de una cédula dominicana
    /// Algoritmo: Módulo 10 con pesos alternados 1,2
    /// </summary>
    private static int CalculateCheckDigit(string first10Digits)
    {
        int[] weights = { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
        int sum = 0;

        for (int i = 0; i < 10; i++)
        {
            int digit = int.Parse(first10Digits[i].ToString());
            int product = digit * weights[i];

            // Si el producto es >= 10, sumar sus dígitos individuales
            sum += product >= 10 ? (product / 10) + (product % 10) : product;
        }

        // El dígito verificador es (10 - (suma mod 10)) mod 10
        return (10 - (sum % 10)) % 10;
    }

    /// <summary>
    /// Limpia una cédula removiendo caracteres no numéricos
    /// </summary>
    public static string CleanCedula(string cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula))
            return string.Empty;

        return new string(cedula.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Formatea una cédula al formato estándar XXX-XXXXXXX-X
    /// </summary>
    public static string FormatCedula(string cedula)
    {
        var cleaned = CleanCedula(cedula);
        if (cleaned.Length != 11)
            return cedula;

        return $"{cleaned.Substring(0, 3)}-{cleaned.Substring(3, 7)}-{cleaned.Substring(10, 1)}";
    }

    /// <summary>
    /// Genera una cédula de ejemplo válida para testing
    /// </summary>
    public static string GenerateTestCedula(int municipio = 1)
    {
        var random = new Random();
        var uniqueNumber = random.Next(0, 9999999).ToString("D7");
        var first10 = $"{municipio:D3}{uniqueNumber}";
        var checkDigit = CalculateCheckDigit(first10);
        return $"{first10}{checkDigit}";
    }

    /// <summary>
    /// Valida la edad a partir de la fecha de nacimiento
    /// </summary>
    public static (bool IsValid, int Age, string? Error) ValidateAge(DateTime dateOfBirth, int minimumAge = 18)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;

        // Ajustar si aún no ha cumplido años este año
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;

        if (age < minimumAge)
            return (false, age, $"La edad mínima requerida es {minimumAge} años (edad actual: {age})");

        if (age > 120)
            return (false, age, "La fecha de nacimiento no es válida");

        return (true, age, null);
    }
}

/// <summary>
/// Resultado detallado de validación de cédula
/// </summary>
public class CedulaValidationResult
{
    public bool IsValid { get; set; }
    public bool FormatValid { get; set; }
    public bool MunicipioValid { get; set; }
    public bool ChecksumValid { get; set; }

    public string CleanedNumber { get; set; } = string.Empty;
    public string FormattedNumber { get; set; } = string.Empty;

    public int Municipio { get; set; }
    public int ExpectedCheckDigit { get; set; }
    public int ActualCheckDigit { get; set; }

    public List<string> Errors { get; set; } = new();
}
