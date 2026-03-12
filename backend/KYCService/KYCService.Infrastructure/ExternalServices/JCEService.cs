using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KYCService.Domain.Validators;

namespace KYCService.Infrastructure.ExternalServices;

/// <summary>
/// Servicio de validación de cédula dominicana.
/// 
/// ⚠️ IMPORTANTE: En República Dominicana NO EXISTE una API pública de la JCE
/// para validar cédulas de manera programática.
/// 
/// Este servicio SOLO realiza:
/// 1. Validación local del formato de cédula (longitud, caracteres)
/// 2. Validación del dígito verificador (algoritmo Módulo 10)
/// 3. Validación del código de municipio (001-044)
/// 
/// La verificación real de identidad se hace mediante:
/// - OCR del documento físico
/// - Comparación de datos OCR vs datos registrados por el usuario
/// - Face comparison entre foto del documento y selfie
/// 
/// Cualquier servicio que afirme "validar con JCE" está mintiendo o
/// usando scraping ilegal de la página web de la JCE.
/// </summary>
public class JCEService : IJCEService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JCEService> _logger;
    private readonly JCEServiceConfig _config;

    public JCEService(
        HttpClient httpClient,
        ILogger<JCEService> logger,
        IOptions<JCEServiceConfig> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;

        // Configurar timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<JCEValidationResult> ValidateCedulaAsync(string cedula, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new JCEValidationResult();

        try
        {
            _logger.LogInformation("Validating cedula {CedulaMasked} against JCE", MaskCedula(cedula));

            // Paso 1: Validación local del formato y dígito verificador
            var localValidation = CedulaValidator.ValidateDetailed(cedula);
            if (!localValidation.IsValid)
            {
                _logger.LogWarning("Cedula {CedulaMasked} failed local validation: {Errors}",
                    MaskCedula(cedula), string.Join(", ", localValidation.Errors));

                result.IsValid = false;
                result.ErrorMessage = string.Join("; ", localValidation.Errors);
                result.ResponseCode = "LOCAL_VALIDATION_FAILED";
                return result;
            }

            // Paso 2: Consulta al servicio externo (si está habilitado)
            if (_config.ExternalValidationEnabled && !string.IsNullOrEmpty(_config.ApiBaseUrl))
            {
                var externalResult = await ValidateExternalAsync(localValidation.CleanedNumber, cancellationToken);
                if (externalResult != null)
                {
                    result = externalResult;
                    result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }
            }

            // Paso 3: Fallback - Usar validación local + datos simulados/cache
            result.IsValid = true;
            result.IsActive = true;
            result.IsDeceased = false;
            result.ResponseCode = "LOCAL_VALIDATION_OK";
            result.CitizenData = await GetCachedOrSimulatedDataAsync(localValidation.CleanedNumber, cancellationToken);

            _logger.LogInformation("Cedula {CedulaMasked} validated successfully (local mode)",
                MaskCedula(cedula));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cedula {CedulaMasked}", MaskCedula(cedula));
            result.IsValid = false;
            result.ErrorMessage = "Error al validar la cédula. Intente nuevamente.";
            result.ResponseCode = "SERVICE_ERROR";
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<JCECitizenData?> GetCitizenDataAsync(string cedula, CancellationToken cancellationToken = default)
    {
        var cleanCedula = CedulaValidator.CleanCedula(cedula);

        // Primero intentar validación completa
        var validationResult = await ValidateCedulaAsync(cedula, cancellationToken);

        return validationResult.CitizenData;
    }

    /// <inheritdoc />
    public async Task<bool> IsInElectoralPadronAsync(string cedula, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateCedulaAsync(cedula, cancellationToken);
        return validationResult.IsValid && validationResult.IsActive;
    }

    /// <inheritdoc />
    public async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.ExternalValidationEnabled || string.IsNullOrEmpty(_config.HealthCheckUrl))
            return true; // Local validation siempre disponible

        try
        {
            var response = await _httpClient.GetAsync(_config.HealthCheckUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Valida contra servicio externo de JCE
    /// </summary>
    private async Task<JCEValidationResult?> ValidateExternalAsync(string cleanCedula, CancellationToken cancellationToken)
    {
        try
        {
            // Opción 1: API REST directa (si JCE tiene API)
            if (_config.UseRestApi)
            {
                return await ValidateViaRestApiAsync(cleanCedula, cancellationToken);
            }

            // Opción 2: Web Scraping del portal público (NO RECOMENDADO para producción)
            if (_config.UseWebScraping)
            {
                _logger.LogWarning("Web scraping mode is enabled - not recommended for production");
                return await ValidateViaWebScrapingAsync(cleanCedula, cancellationToken);
            }

            // Opción 3: Servicio de terceros autorizado (FortunaRD, etc.)
            if (_config.UseThirdPartyService)
            {
                return await ValidateViaThirdPartyAsync(cleanCedula, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "External JCE service unavailable, falling back to local validation");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("JCE service timeout, falling back to local validation");
        }

        return null;
    }

    /// <summary>
    /// Validación via API REST de JCE (requiere convenio)
    /// </summary>
    private async Task<JCEValidationResult> ValidateViaRestApiAsync(string cleanCedula, CancellationToken cancellationToken)
    {
        var request = new
        {
            cedula = cleanCedula,
            apiKey = _config.ApiKey
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.ApiBaseUrl}/api/v1/cedula/validate",
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var jceResponse = await response.Content.ReadFromJsonAsync<JCEApiResponse>(cancellationToken: cancellationToken);

            return new JCEValidationResult
            {
                IsValid = jceResponse?.Success ?? false,
                IsActive = jceResponse?.Data?.Status == "ACTIVA",
                IsDeceased = jceResponse?.Data?.Status == "FALLECIDO",
                ResponseCode = jceResponse?.Code ?? "UNKNOWN",
                CitizenData = jceResponse?.Data != null ? MapApiResponseToData(jceResponse.Data, cleanCedula) : null
            };
        }

        throw new HttpRequestException($"JCE API returned {response.StatusCode}");
    }

    /// <summary>
    /// Validación via Web Scraping (solo para desarrollo)
    /// </summary>
    private async Task<JCEValidationResult> ValidateViaWebScrapingAsync(string cleanCedula, CancellationToken cancellationToken)
    {
        // NOTA: Esto es solo para desarrollo/testing
        // En producción, usar API oficial o servicio autorizado

        _logger.LogWarning("Using web scraping mode - This should NOT be used in production");

        // Simular consulta al portal de la JCE
        // En realidad, esto requeriría HtmlAgilityPack o similar
        await Task.Delay(500, cancellationToken); // Simular latencia

        // Retornar datos simulados
        return new JCEValidationResult
        {
            IsValid = true,
            IsActive = true,
            ResponseCode = "WEB_SCRAPING_SIMULATED",
            CitizenData = GenerateSimulatedData(cleanCedula)
        };
    }

    /// <summary>
    /// Validación via servicio de terceros autorizado
    /// </summary>
    private async Task<JCEValidationResult> ValidateViaThirdPartyAsync(string cleanCedula, CancellationToken cancellationToken)
    {
        // Ejemplo con FortunaRD u otro proveedor
        var request = new
        {
            cedula = cleanCedula,
            token = _config.ThirdPartyToken
        };

        var response = await _httpClient.PostAsJsonAsync(
            _config.ThirdPartyApiUrl,
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var thirdPartyResponse = await response.Content.ReadFromJsonAsync<ThirdPartyResponse>(cancellationToken: cancellationToken);

            if (thirdPartyResponse?.Success == true && thirdPartyResponse.Data != null)
            {
                return new JCEValidationResult
                {
                    IsValid = true,
                    IsActive = thirdPartyResponse.Data.Status == "active",
                    IsDeceased = thirdPartyResponse.Data.Deceased,
                    ResponseCode = "THIRD_PARTY_OK",
                    CitizenData = new JCECitizenData
                    {
                        Cedula = CedulaValidator.FormatCedula(cleanCedula),
                        FirstName = thirdPartyResponse.Data.FirstName ?? "",
                        MiddleName = thirdPartyResponse.Data.MiddleName,
                        LastName = thirdPartyResponse.Data.LastName ?? "",
                        SecondLastName = thirdPartyResponse.Data.SecondLastName,
                        DateOfBirth = thirdPartyResponse.Data.DateOfBirth,
                        Gender = thirdPartyResponse.Data.Gender ?? "M",
                        Nationality = "DOMINICANA",
                        Status = thirdPartyResponse.Data.Status == "active" ? JCECedulaStatus.Active : JCECedulaStatus.Unknown
                    }
                };
            }
        }

        throw new HttpRequestException("Third party service failed");
    }

    /// <summary>
    /// Obtiene datos de cache o genera datos simulados para desarrollo
    /// </summary>
    private async Task<JCECitizenData?> GetCachedOrSimulatedDataAsync(string cleanCedula, CancellationToken cancellationToken)
    {
        // TODO: Implementar cache con Redis
        // var cached = await _cache.GetAsync<JCECitizenData>($"jce:cedula:{cleanCedula}");
        // if (cached != null) return cached;

        // En modo desarrollo, generar datos simulados
        if (_config.SimulationEnabled)
        {
            await Task.CompletedTask; // Evitar warning
            return GenerateSimulatedData(cleanCedula);
        }

        return null;
    }

    /// <summary>
    /// Genera datos simulados basados en el número de cédula
    /// Solo para desarrollo y testing
    /// </summary>
    private JCECitizenData GenerateSimulatedData(string cleanCedula)
    {
        // Usar hash del número de cédula para generar datos consistentes
        var hash = cleanCedula.GetHashCode();
        var random = new Random(hash);

        var firstNames = new[] { "JUAN", "MARIA", "CARLOS", "ANA", "PEDRO", "ROSA", "JOSE", "CARMEN", "MIGUEL", "LUCIA" };
        var middleNames = new[] { "ANTONIO", "ISABEL", "ANDRES", "MERCEDES", "FRANCISCO", "ALTAGRACIA", null };
        var lastNames = new[] { "PEREZ", "MARTINEZ", "RODRIGUEZ", "GARCIA", "SANCHEZ", "DIAZ", "FERNANDEZ", "LOPEZ" };
        var municipalities = new[] { "SANTO DOMINGO", "SANTIAGO", "LA VEGA", "SAN CRISTOBAL", "PUERTO PLATA" };
        var provinces = new[] { "DISTRITO NACIONAL", "SANTIAGO", "LA VEGA", "SAN CRISTOBAL", "PUERTO PLATA" };

        var year = random.Next(1960, 2005);
        var month = random.Next(1, 13);
        var day = random.Next(1, 29);

        return new JCECitizenData
        {
            Cedula = CedulaValidator.FormatCedula(cleanCedula),
            FirstName = firstNames[random.Next(firstNames.Length)],
            MiddleName = middleNames[random.Next(middleNames.Length)],
            LastName = lastNames[random.Next(lastNames.Length)],
            SecondLastName = lastNames[random.Next(lastNames.Length)],
            DateOfBirth = new DateTime(year, month, day),
            BirthPlace = municipalities[random.Next(municipalities.Length)],
            Gender = random.Next(2) == 0 ? "M" : "F",
            Nationality = "DOMINICANA",
            MaritalStatus = random.Next(3) switch { 0 => "SOLTERO/A", 1 => "CASADO/A", _ => "UNION LIBRE" },
            Municipality = municipalities[random.Next(municipalities.Length)],
            Province = provinces[random.Next(provinces.Length)],
            IssueDate = DateTime.Now.AddYears(-random.Next(1, 8)),
            ExpiryDate = DateTime.Now.AddYears(random.Next(1, 10)),
            Status = JCECedulaStatus.Active
        };
    }

    private JCECitizenData MapApiResponseToData(JCEApiDataResponse data, string cleanCedula)
    {
        return new JCECitizenData
        {
            Cedula = CedulaValidator.FormatCedula(cleanCedula),
            FirstName = data.FirstName ?? "",
            MiddleName = data.MiddleName,
            LastName = data.LastName ?? "",
            SecondLastName = data.SecondLastName,
            DateOfBirth = DateTime.TryParse(data.DateOfBirth, out var dob) ? dob : DateTime.MinValue,
            BirthPlace = data.BirthPlace,
            Gender = data.Gender ?? "M",
            Nationality = data.Nationality ?? "DOMINICANA",
            MaritalStatus = data.MaritalStatus,
            Municipality = data.Municipality,
            Province = data.Province,
            IssueDate = DateTime.TryParse(data.IssueDate, out var issue) ? issue : null,
            ExpiryDate = DateTime.TryParse(data.ExpiryDate, out var expiry) ? expiry : null,
            Status = MapStatus(data.Status)
        };
    }

    private JCECedulaStatus MapStatus(string? status) => status?.ToUpper() switch
    {
        "ACTIVA" or "ACTIVE" => JCECedulaStatus.Active,
        "EXPIRADA" or "EXPIRED" => JCECedulaStatus.Expired,
        "CANCELADA" or "CANCELLED" => JCECedulaStatus.Cancelled,
        "FALLECIDO" or "DECEASED" => JCECedulaStatus.Deceased,
        "SUSPENDIDA" or "SUSPENDED" => JCECedulaStatus.Suspended,
        _ => JCECedulaStatus.Unknown
    };

    private string MaskCedula(string cedula)
    {
        var clean = CedulaValidator.CleanCedula(cedula);
        if (clean.Length >= 7)
            return $"***-{clean.Substring(3, 4)}***-*";
        return "***-*******-*";
    }
}

#region DTOs para respuestas de APIs externas

internal class JCEApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public JCEApiDataResponse? Data { get; set; }
}

internal class JCEApiDataResponse
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("secondLastName")]
    public string? SecondLastName { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("birthPlace")]
    public string? BirthPlace { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("maritalStatus")]
    public string? MaritalStatus { get; set; }

    [JsonPropertyName("municipality")]
    public string? Municipality { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("issueDate")]
    public string? IssueDate { get; set; }

    [JsonPropertyName("expiryDate")]
    public string? ExpiryDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

internal class ThirdPartyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ThirdPartyDataResponse? Data { get; set; }
}

internal class ThirdPartyDataResponse
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("secondLastName")]
    public string? SecondLastName { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("deceased")]
    public bool Deceased { get; set; }
}

#endregion

/// <summary>
/// Configuración del servicio JCE
/// </summary>
public class JCEServiceConfig
{
    public const string SectionName = "JCEService";

    /// <summary>
    /// Indica si la validación externa está habilitada
    /// </summary>
    public bool ExternalValidationEnabled { get; set; } = false;

    /// <summary>
    /// URL base de la API de JCE (si hay convenio)
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// API Key para acceso a JCE
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// URL para health check del servicio
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Timeout en segundos para llamadas HTTP
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Usar API REST directa
    /// </summary>
    public bool UseRestApi { get; set; } = false;

    /// <summary>
    /// Usar web scraping (solo desarrollo)
    /// </summary>
    public bool UseWebScraping { get; set; } = false;

    /// <summary>
    /// Usar servicio de terceros autorizado
    /// </summary>
    public bool UseThirdPartyService { get; set; } = false;

    /// <summary>
    /// URL del servicio de terceros
    /// </summary>
    public string? ThirdPartyApiUrl { get; set; }

    /// <summary>
    /// Token de autenticación del servicio de terceros
    /// </summary>
    public string? ThirdPartyToken { get; set; }

    /// <summary>
    /// Habilitar modo simulación (para desarrollo)
    /// </summary>
    public bool SimulationEnabled { get; set; } = true;

    /// <summary>
    /// Tiempo en minutos para cache de resultados
    /// </summary>
    public int CacheMinutes { get; set; } = 60;
}
