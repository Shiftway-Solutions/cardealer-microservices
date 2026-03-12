using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using CarDealer.Shared.Caching.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace VehiclesSaleService.Api.Controllers;

/// <summary>
/// Controller para el catálogo maestro de vehículos.
/// Proporciona datos para auto-completar formularios de publicación.
/// Endpoints: makes (by slug/UUID/name), models (by slug/UUID/name), VIN decode.
/// 
/// Flujo de uso:
/// 1. GET /catalog/makes - Lista de marcas
/// 2. GET /catalog/makes/{makeSlug}/models - Modelos de una marca
/// 3. GET /catalog/models/{modelId}/years - Años disponibles para un modelo
/// 4. GET /catalog/models/{modelId}/years/{year}/trims - Trims con specs completas
/// 5. El frontend auto-llena el formulario con las specs del trim seleccionado
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("VehiclesPolicy")]
public class CatalogController : ControllerBase
{
    private readonly IVehicleCatalogRepository _catalogRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ILogger<CatalogController> _logger;
    private readonly ICacheService _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    public CatalogController(
        IVehicleCatalogRepository catalogRepository,
        IVehicleRepository vehicleRepository,
        ILogger<CatalogController> logger,
        ICacheService cache,
        IHttpClientFactory httpClientFactory)
    {
        _catalogRepository = catalogRepository;
        _vehicleRepository = vehicleRepository;
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
    }

    // ========================================
    // MAKES (Marcas)
    // ========================================

    /// <summary>
    /// Obtiene todas las marcas de vehículos activas.
    /// </summary>
    /// <remarks>
    /// Ejemplo: GET /api/catalog/makes
    /// Retorna: Toyota, Honda, Ford, Chevrolet, etc.
    /// Las marcas populares aparecen primero.
    /// </remarks>
    [HttpGet("makes")]
    [ProducesResponseType(typeof(IEnumerable<MakeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MakeDto>>> GetAllMakes()
    {
        const string cacheKey = "catalog:makes:all";
        var cached = await _cache.GetAsync<List<MakeDto>>(cacheKey);
        if (cached is not null)
            return Ok(cached);

        var makes = await _catalogRepository.GetAllMakesAsync();
        var dtos = makes.Select(m => new MakeDto
        {
            Id = m.Id,
            Name = m.Name,
            Slug = m.Slug,
            LogoUrl = m.LogoUrl,
            Country = m.Country,
            IsPopular = m.IsPopular
        }).ToList();

        await _cache.SetAsync(cacheKey, dtos, ttlSeconds: 86400); // 24h
        return Ok(dtos);
    }

    /// <summary>
    /// Obtiene las marcas más populares (para mostrar primero en UI).
    /// </summary>
    [HttpGet("makes/popular")]
    [ProducesResponseType(typeof(IEnumerable<MakeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MakeDto>>> GetPopularMakes([FromQuery] int take = 20)
    {
        var cacheKey = $"catalog:makes:popular:{take}";
        var cached = await _cache.GetAsync<List<MakeDto>>(cacheKey);
        if (cached is not null)
            return Ok(cached);

        var makes = await _catalogRepository.GetPopularMakesAsync(take);
        var dtos = makes.Select(m => new MakeDto
        {
            Id = m.Id,
            Name = m.Name,
            Slug = m.Slug,
            LogoUrl = m.LogoUrl,
            Country = m.Country,
            IsPopular = m.IsPopular
        }).ToList();

        await _cache.SetAsync(cacheKey, dtos, ttlSeconds: 86400); // 24h
        return Ok(dtos);
    }

    /// <summary>
    /// Busca marcas por nombre (para autocomplete).
    /// </summary>
    [HttpGet("makes/search")]
    [ProducesResponseType(typeof(IEnumerable<MakeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MakeDto>>> SearchMakes([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Enumerable.Empty<MakeDto>());

        var makes = await _catalogRepository.SearchMakesAsync(q);
        var dtos = makes.Select(m => new MakeDto
        {
            Id = m.Id,
            Name = m.Name,
            Slug = m.Slug,
            LogoUrl = m.LogoUrl,
            Country = m.Country,
            IsPopular = m.IsPopular
        });
        return Ok(dtos);
    }

    // ========================================
    // MODELS (Modelos)
    // ========================================

    /// <summary>
    /// Obtiene todos los modelos de una marca por slug, UUID o nombre.
    /// </summary>
    /// <remarks>
    /// Ejemplo: GET /api/catalog/makes/toyota/models           (slug)
    /// Ejemplo: GET /api/catalog/makes/HONDA/models            (name, case-insensitive)
    /// Ejemplo: GET /api/catalog/makes/{uuid}/models           (UUID from VIN decode)
    /// Retorna: Camry, Corolla, RAV4, Tacoma, etc.
    /// </remarks>
    [HttpGet("makes/{makeSlug}/models")]
    [ProducesResponseType(typeof(IEnumerable<ModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ModelDto>>> GetModelsByMake(string makeSlug)
    {
        VehicleMake? make = null;

        // 1. Try UUID (from VIN decode catalogMakeId)
        if (Guid.TryParse(makeSlug, out var makeGuid))
        {
            make = await _catalogRepository.GetMakeByIdAsync(makeGuid);
        }

        // 2. Try exact slug (e.g. "toyota")
        if (make == null)
        {
            make = await _catalogRepository.GetMakeBySlugAsync(makeSlug);
        }

        // 3. Try lowercase slug (e.g. "HONDA" → "honda")
        if (make == null && makeSlug != makeSlug.ToLowerInvariant())
        {
            make = await _catalogRepository.GetMakeBySlugAsync(makeSlug.ToLowerInvariant());
        }

        // 4. Try by name (case-insensitive search, e.g. "HONDA" → "Honda")
        if (make == null)
        {
            var searchResults = await _catalogRepository.SearchMakesAsync(makeSlug);
            make = searchResults.FirstOrDefault(m =>
                m.Name.Equals(makeSlug, StringComparison.OrdinalIgnoreCase));
        }

        if (make == null)
            return NotFound(new { message = $"Make '{makeSlug}' not found" });

        var models = await _catalogRepository.GetModelsByMakeIdAsync(make.Id);
        var dtos = models.Select(m => new ModelDto
        {
            Id = m.Id,
            MakeId = m.MakeId,
            MakeName = make.Name,
            Name = m.Name,
            Slug = m.Slug,
            VehicleType = m.VehicleType.ToString(),
            BodyStyle = m.DefaultBodyStyle?.ToString(),
            StartYear = m.StartYear,
            EndYear = m.EndYear,
            IsPopular = m.IsPopular
        });
        return Ok(dtos);
    }

    /// <summary>
    /// Busca modelos dentro de una marca (para autocomplete).
    /// </summary>
    [HttpGet("makes/{makeId:guid}/models/search")]
    [ProducesResponseType(typeof(IEnumerable<ModelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ModelDto>>> SearchModels(Guid makeId, [FromQuery] string q)
    {
        var models = await _catalogRepository.SearchModelsAsync(makeId, q);
        var dtos = models.Select(m => new ModelDto
        {
            Id = m.Id,
            MakeId = m.MakeId,
            MakeName = m.Make?.Name ?? "",
            Name = m.Name,
            Slug = m.Slug,
            VehicleType = m.VehicleType.ToString(),
            BodyStyle = m.DefaultBodyStyle?.ToString(),
            StartYear = m.StartYear,
            EndYear = m.EndYear,
            IsPopular = m.IsPopular
        });
        return Ok(dtos);
    }

    // ========================================
    // YEARS (Años disponibles)
    // ========================================

    /// <summary>
    /// Obtiene los años disponibles para un modelo.
    /// </summary>
    /// <remarks>
    /// Ejemplo: GET /api/catalog/models/{modelId}/years
    /// Retorna: [2024, 2023, 2022, 2021, 2020, ...]
    /// </remarks>
    [HttpGet("models/{modelId:guid}/years")]
    [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<int>>> GetAvailableYears(Guid modelId)
    {
        var model = await _catalogRepository.GetModelByIdAsync(modelId);
        if (model == null)
            return NotFound(new { message = "Model not found" });

        var years = await _catalogRepository.GetAvailableYearsAsync(modelId);
        return Ok(years);
    }

    // ========================================
    // TRIMS (Versiones) - Auto-fill specs
    // ========================================

    /// <summary>
    /// Obtiene todos los trims/versiones disponibles para un modelo y año.
    /// Este endpoint retorna las especificaciones completas de cada trim.
    /// </summary>
    /// <remarks>
    /// Ejemplo: GET /api/catalog/models/{modelId}/years/2024/trims
    /// Retorna lista de trims con specs para auto-llenar el formulario:
    /// - LE: 2.5L, 203hp, FWD, 28/39 MPG, $28,400
    /// - SE: 2.5L, 203hp, FWD, 28/39 MPG, $29,495
    /// - XLE: 2.5L, 203hp, FWD, 28/39 MPG, $31,170
    /// - TRD: 3.5L V6, 301hp, FWD, 22/33 MPG, $36,095
    /// - Hybrid LE: 2.5L Hybrid, 225hp, AWD, 51/53 MPG, $29,845
    /// </remarks>
    [HttpGet("models/{modelId:guid}/years/{year:int}/trims")]
    [ProducesResponseType(typeof(IEnumerable<TrimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TrimDto>>> GetTrimsByModelAndYear(Guid modelId, int year)
    {
        var model = await _catalogRepository.GetModelByIdAsync(modelId);
        if (model == null)
            return NotFound(new { message = "Model not found" });

        var trims = await _catalogRepository.GetTrimsByModelAndYearAsync(modelId, year);
        var dtos = trims.Select(t => new TrimDto
        {
            Id = t.Id,
            ModelId = t.ModelId,
            MakeName = t.Model?.Make?.Name ?? "",
            ModelName = t.Model?.Name ?? "",
            Name = t.Name,
            Slug = t.Slug,
            Year = t.Year,

            // Specs para auto-fill
            EngineSize = t.EngineSize,
            Horsepower = t.Horsepower,
            Torque = t.Torque,
            FuelType = t.FuelType?.ToString(),
            Transmission = t.Transmission?.ToString(),
            DriveType = t.DriveType?.ToString(),

            // Fuel economy
            MpgCity = t.MpgCity,
            MpgHighway = t.MpgHighway,
            MpgCombined = t.MpgCombined,

            // Precio base de referencia
            BaseMSRP = t.BaseMSRP
        });
        return Ok(dtos);
    }

    /// <summary>
    /// Obtiene las especificaciones completas de un trim específico.
    /// Usado para auto-llenar TODOS los campos del formulario de publicación.
    /// </summary>
    [HttpGet("trims/{trimId:guid}")]
    [ProducesResponseType(typeof(TrimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrimDto>> GetTrimById(Guid trimId)
    {
        var trim = await _catalogRepository.GetTrimByIdAsync(trimId);
        if (trim == null)
            return NotFound(new { message = "Trim not found" });

        return Ok(new TrimDto
        {
            Id = trim.Id,
            ModelId = trim.ModelId,
            MakeName = trim.Model?.Make?.Name ?? "",
            ModelName = trim.Model?.Name ?? "",
            Name = trim.Name,
            Slug = trim.Slug,
            Year = trim.Year,
            EngineSize = trim.EngineSize,
            Horsepower = trim.Horsepower,
            Torque = trim.Torque,
            FuelType = trim.FuelType?.ToString(),
            Transmission = trim.Transmission?.ToString(),
            DriveType = trim.DriveType?.ToString(),
            MpgCity = trim.MpgCity,
            MpgHighway = trim.MpgHighway,
            MpgCombined = trim.MpgCombined,
            BaseMSRP = trim.BaseMSRP
        });
    }

    // ========================================
    // VIN DECODING
    // ========================================

    /// <summary>
    /// Decode a Vehicle Identification Number (VIN) to extract vehicle information.
    /// Uses NHTSA VPIC API for decoding.
    /// </summary>
    /// <remarks>
    /// Example VIN: 1HGCV1F32LA000001 (2020 Honda Accord)
    /// 
    /// Returns decoded information:
    /// - Make, Model, Year
    /// - Body Style, Vehicle Type
    /// - Engine Size, Fuel Type
    /// - Transmission, Drive Type
    /// - Manufacturing Location
    /// </remarks>
    [HttpGet("vin/{vin}/decode")]
    [ProducesResponseType(typeof(VinDecodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VinDecodeResponse>> DecodeVin(string vin)
    {
        // Validate VIN format (17 characters, alphanumeric except I, O, Q)
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
        {
            return BadRequest(new { message = "Invalid VIN format. VIN must be exactly 17 characters." });
        }

        vin = vin.ToUpperInvariant();

        // Check for invalid characters (I, O, Q are not used in VINs)
        if (vin.Any(c => c == 'I' || c == 'O' || c == 'Q'))
        {
            return BadRequest(new { message = "Invalid VIN. VINs cannot contain I, O, or Q characters." });
        }

        try
        {
            // Call NHTSA VPIC API (uses IHttpClientFactory for DNS rotation + resilience)
            var nhtsaUrl = $"https://vpic.nhtsa.dot.gov/api/vehicles/decodevinvalues/{vin}?format=json";

            _logger.LogInformation("Decoding VIN: {VIN} via NHTSA API", vin);

            var httpClient = _httpClientFactory.CreateClient("NHTSA");
            var response = await httpClient.GetAsync(nhtsaUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var nhtsaResponse = System.Text.Json.JsonSerializer.Deserialize<NhtsaVinResponse>(content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (nhtsaResponse?.Results == null || !nhtsaResponse.Results.Any())
            {
                return BadRequest(new { message = "Unable to decode VIN. No results returned from NHTSA." });
            }

            var result = nhtsaResponse.Results.First();

            // Check for error codes
            if (!string.IsNullOrEmpty(result.ErrorCode) && result.ErrorCode != "0")
            {
                _logger.LogWarning("VIN decode returned error: {ErrorCode} - {ErrorText}",
                    result.ErrorCode, result.ErrorText);
            }

            // Parse year
            int.TryParse(result.ModelYear, out var year);

            // Parse engine info
            int.TryParse(result.EngineHP, out var horsepower);
            int.TryParse(result.EngineCylinders, out var cylinders);

            // Map fuel type
            var fuelType = MapFuelType(result.FuelTypePrimary);
            var transmission = MapTransmission(result.TransmissionStyle);
            var driveType = MapDriveType(result.DriveType);
            var bodyStyle = MapBodyStyle(result.BodyClass);
            var vehicleType = MapVehicleType(result.VehicleType);

            var vinResponse = new VinDecodeResponse
            {
                VIN = vin,
                IsValid = string.IsNullOrEmpty(result.ErrorCode) || result.ErrorCode == "0",

                // Basic Info
                Make = result.Make ?? string.Empty,
                Model = result.Model ?? string.Empty,
                Year = year,
                Trim = result.Trim,

                // Type & Body
                VehicleType = vehicleType,
                BodyStyle = bodyStyle,
                Doors = ParseInt(result.Doors),

                // Engine
                EngineSize = result.DisplacementL != null ? $"{result.DisplacementL}L" : result.EngineModel,
                Cylinders = cylinders > 0 ? cylinders : null,
                Horsepower = horsepower > 0 ? horsepower : null,
                FuelType = fuelType,

                // Transmission & Drive
                Transmission = transmission,
                DriveType = driveType,

                // Manufacturing
                PlantCity = result.PlantCity,
                PlantCountry = result.PlantCountry,
                Manufacturer = result.Manufacturer,

                // Additional
                Series = result.Series,
                GVWR = result.GVWR,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorText,

                // Suggested data for form auto-fill
                SuggestedData = new VinSuggestedData
                {
                    Make = result.Make ?? string.Empty,
                    Model = result.Model ?? string.Empty,
                    Year = year,
                    Trim = result.Trim,
                    VehicleType = vehicleType,
                    BodyStyle = bodyStyle,
                    FuelType = fuelType,
                    Transmission = transmission,
                    DriveType = driveType,
                    EngineSize = result.DisplacementL != null ? $"{result.DisplacementL}L" : null,
                    Horsepower = horsepower > 0 ? horsepower : null,
                    Cylinders = cylinders > 0 ? cylinders : null
                }
            };

            _logger.LogInformation("VIN decoded successfully: {VIN} -> {Year} {Make} {Model}",
                vin, year, result.Make, result.Model);

            return Ok(vinResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call NHTSA API for VIN: {VIN}", vin);
            return BadRequest(new { message = "Failed to decode VIN. NHTSA service unavailable." });
        }
    }

    // ========================================
    // SMART VIN DECODING (Enhanced)
    // ========================================

    /// <summary>
    /// Smart VIN decode with catalog matching, duplicate check, and auto-generated description.
    /// Returns enriched data for instant form auto-fill.
    /// </summary>
    [HttpGet("vin/{vin}/decode-smart")]
    [ProducesResponseType(typeof(SmartVinDecodeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmartVinDecodeResult>> DecodeVinSmart(string vin)
    {
        // 1. Validate VIN format
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            return BadRequest(new { message = "El VIN debe tener exactamente 17 caracteres." });

        vin = vin.ToUpperInvariant().Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(vin, @"^[A-HJ-NPR-Z0-9]{17}$"))
            return BadRequest(new { message = "VIN contiene caracteres inválidos. No se permiten I, O, Q." });

        // 2. Validate checksum (digit 9)
        if (!ValidateVinChecksum(vin))
        {
            _logger.LogWarning("VIN checksum validation failed for: {VIN}", vin);
            // Don't reject, just log — some older VINs may not pass
        }

        try
        {
            // 3. Check for duplicate VIN in database
            bool isDuplicate = false;
            Guid? existingVehicleId = null;
            string? existingVehicleSlug = null;

            var existingVehicle = await _vehicleRepository.GetByVINAsync(vin);
            if (existingVehicle != null)
            {
                isDuplicate = true;
                existingVehicleId = existingVehicle.Id;
                existingVehicleSlug = $"{existingVehicle.Year}-{existingVehicle.Make}-{existingVehicle.Model}"
                    .ToLowerInvariant().Replace(" ", "-").Replace("--", "-")
                    + "-" + existingVehicle.Id.ToString("N")[..8];
            }

            // 4. Call NHTSA VPIC API
            var nhtsaUrl = $"https://vpic.nhtsa.dot.gov/api/vehicles/decodevinvalues/{vin}?format=json";
            _logger.LogInformation("Smart VIN decode: {VIN} via NHTSA API", vin);

            var httpClient = _httpClientFactory.CreateClient("NHTSA");
            var response = await httpClient.GetAsync(nhtsaUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var nhtsaResponse = System.Text.Json.JsonSerializer.Deserialize<NhtsaVinResponse>(content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (nhtsaResponse?.Results == null || !nhtsaResponse.Results.Any())
                return BadRequest(new { message = "No se pudo decodificar el VIN. NHTSA no retornó resultados." });

            var nhtsaResult = nhtsaResponse.Results.First();

            // Parse NHTSA data
            int.TryParse(nhtsaResult.ModelYear, out var year);
            int.TryParse(nhtsaResult.EngineHP, out var horsepower);
            int.TryParse(nhtsaResult.EngineCylinders, out var cylinders);

            var fuelType = MapFuelType(nhtsaResult.FuelTypePrimary);
            var transmission = MapTransmission(nhtsaResult.TransmissionStyle);
            var driveType = MapDriveType(nhtsaResult.DriveType);
            var bodyStyle = MapBodyStyle(nhtsaResult.BodyClass);
            var vehicleType = MapVehicleType(nhtsaResult.VehicleType);
            var doors = ParseInt(nhtsaResult.Doors);
            var engineSize = nhtsaResult.DisplacementL != null ? $"{nhtsaResult.DisplacementL}L" : nhtsaResult.EngineModel;
            var makeName = nhtsaResult.Make ?? string.Empty;
            var modelName = nhtsaResult.Model ?? string.Empty;
            var trimName = nhtsaResult.Trim;

            // 5. Match against local catalog (fuzzy match) — auto-inserts make/model when not found
            Guid? catalogMakeId = null;
            Guid? catalogModelId = null;
            Guid? catalogTrimId = null;
            bool hasCatalogMatch = false;

            if (!string.IsNullOrWhiteSpace(makeName))
            {
                var makes = await _catalogRepository.SearchMakesAsync(makeName);
                var matchedMake = makes.FirstOrDefault(m =>
                    m.Name.Equals(makeName, StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains(makeName, StringComparison.OrdinalIgnoreCase) ||
                    makeName.Contains(m.Name, StringComparison.OrdinalIgnoreCase));

                // Auto-create the make if not found in catalog
                if (matchedMake == null)
                {
                    _logger.LogInformation("VIN {VIN}: Make '{Make}' not found in catalog — auto-adding", vin, makeName);
                    matchedMake = await _catalogRepository.UpsertMakeAsync(new VehicleMake
                    {
                        Name = makeName,
                        Slug = ToSlug(makeName),
                        Country = nhtsaResult.PlantCountry,
                        IsActive = true,
                        IsPopular = false
                    });
                }

                catalogMakeId = matchedMake.Id;
                hasCatalogMatch = true;

                if (!string.IsNullOrWhiteSpace(modelName))
                {
                    var models = await _catalogRepository.GetModelsByMakeIdAsync(matchedMake.Id);
                    var matchedModel = models.FirstOrDefault(m =>
                        m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains(modelName, StringComparison.OrdinalIgnoreCase) ||
                        modelName.Contains(m.Name, StringComparison.OrdinalIgnoreCase));

                    // Auto-create the model if not found under this make
                    if (matchedModel == null)
                    {
                        _logger.LogInformation("VIN {VIN}: Model '{Model}' not found for make '{Make}' — auto-adding", vin, modelName, makeName);
                        matchedModel = await _catalogRepository.UpsertModelAsync(new VehicleModel
                        {
                            MakeId = matchedMake.Id,
                            Name = modelName,
                            Slug = ToSlug(modelName),
                            VehicleType = ParseVehicleTypeEnum(vehicleType),
                            DefaultBodyStyle = ParseBodyStyleEnum(bodyStyle),
                            StartYear = year > 0 ? year : null,
                            IsActive = true,
                            IsPopular = false
                        });
                    }

                    catalogModelId = matchedModel.Id;

                    if (year > 0)
                    {
                        var trims = await _catalogRepository.GetTrimsByModelAndYearAsync(matchedModel.Id, year);
                        var matchedTrim = trims.FirstOrDefault(t =>
                            !string.IsNullOrWhiteSpace(trimName) &&
                            (t.Name.Equals(trimName, StringComparison.OrdinalIgnoreCase) ||
                             t.Name.Contains(trimName, StringComparison.OrdinalIgnoreCase)));

                        if (matchedTrim != null)
                        {
                            catalogTrimId = matchedTrim.Id;
                            // Enrich with trim specs if NHTSA data was incomplete
                            if (horsepower == 0 && matchedTrim.Horsepower.HasValue)
                                horsepower = matchedTrim.Horsepower.Value;
                            if (string.IsNullOrEmpty(engineSize) && !string.IsNullOrEmpty(matchedTrim.EngineSize))
                                engineSize = matchedTrim.EngineSize;
                        }
                    }
                }
            }

            // 6. Build field confidences
            var confidences = new Dictionary<string, FieldConfidence>
            {
                ["make"] = new(makeName, "NHTSA", string.IsNullOrEmpty(makeName) ? 0.0 : 0.95),
                ["model"] = new(modelName, "NHTSA", string.IsNullOrEmpty(modelName) ? 0.0 : 0.95),
                ["year"] = new(year.ToString(), "NHTSA", year > 0 ? 0.99 : 0.0),
                ["trim"] = new(trimName ?? "", "NHTSA", string.IsNullOrEmpty(trimName) ? 0.0 : 0.8),
                ["bodyStyle"] = new(bodyStyle, "NHTSA", 0.85),
                ["vehicleType"] = new(vehicleType, "NHTSA", 0.9),
                ["fuelType"] = new(fuelType, "NHTSA", string.IsNullOrEmpty(nhtsaResult.FuelTypePrimary) ? 0.5 : 0.9),
                ["transmission"] = new(transmission, "NHTSA", string.IsNullOrEmpty(nhtsaResult.TransmissionStyle) ? 0.5 : 0.85),
                ["driveType"] = new(driveType, "NHTSA", string.IsNullOrEmpty(nhtsaResult.DriveType) ? 0.5 : 0.85),
                ["engineSize"] = new(engineSize ?? "", "NHTSA", string.IsNullOrEmpty(engineSize) ? 0.0 : 0.9),
                ["horsepower"] = new(horsepower.ToString(), "NHTSA", horsepower > 0 ? 0.85 : 0.0),
                ["cylinders"] = new(cylinders.ToString(), "NHTSA", cylinders > 0 ? 0.9 : 0.0),
            };

            // 7. Generate auto-description
            var description = GenerateVehicleDescription(year, makeName, modelName, trimName,
                engineSize, cylinders, horsepower, fuelType, transmission, driveType);

            // 8. Build result
            var smartResult = new SmartVinDecodeResult
            {
                VIN = vin,
                Make = makeName,
                Model = modelName,
                Year = year > 0 ? year : null,
                Trim = trimName,
                BodyStyle = bodyStyle,
                VehicleType = vehicleType,
                EngineSize = engineSize,
                Cylinders = cylinders > 0 ? cylinders : null,
                Horsepower = horsepower > 0 ? horsepower : null,
                FuelType = fuelType,
                Transmission = transmission,
                DriveType = driveType,
                Doors = doors,
                ManufacturedIn = nhtsaResult.PlantCity,
                PlantCountry = nhtsaResult.PlantCountry,
                CatalogMakeId = catalogMakeId,
                CatalogModelId = catalogModelId,
                CatalogTrimId = catalogTrimId,
                HasCatalogMatch = hasCatalogMatch,
                IsDuplicate = isDuplicate,
                ExistingVehicleId = existingVehicleId,
                ExistingVehicleSlug = existingVehicleSlug,
                FieldConfidences = confidences,
                SuggestedDescription = description,
                AutoFill = new VinSuggestedData
                {
                    Make = makeName,
                    Model = modelName,
                    Year = year,
                    Trim = trimName,
                    VehicleType = vehicleType,
                    BodyStyle = bodyStyle,
                    FuelType = fuelType,
                    Transmission = transmission,
                    DriveType = driveType,
                    EngineSize = engineSize,
                    Horsepower = horsepower > 0 ? horsepower : null,
                    Cylinders = cylinders > 0 ? cylinders : null
                }
            };

            _logger.LogInformation("Smart VIN decoded: {VIN} → {Year} {Make} {Model} | Catalog match: {HasMatch} | Duplicate: {IsDup}",
                vin, year, makeName, modelName, hasCatalogMatch, isDuplicate);

            return Ok(smartResult);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "NHTSA API call failed for VIN: {VIN}", vin);
            return BadRequest(new { message = "No se pudo decodificar el VIN. Servicio NHTSA no disponible." });
        }
    }

    /// <summary>
    /// Batch VIN decode for dealers. Max 50 VINs per request.
    /// </summary>
    [HttpPost("vin/decode-batch")]
    [Authorize]
    [ProducesResponseType(typeof(BatchVinDecodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchVinDecodeResponse>> DecodeVinBatch([FromBody] BatchVinDecodeRequest request)
    {
        if (request.Vins == null || !request.Vins.Any())
            return BadRequest(new { message = "Debe proporcionar al menos un VIN." });

        var maxItems = Math.Min(request.MaxItems ?? 50, 50);
        var vins = request.Vins.Take(maxItems).ToList();

        _logger.LogInformation("Batch VIN decode requested for {Count} VINs", vins.Count);

        var results = new List<SmartVinDecodeResult>();
        var errors = new Dictionary<string, string>();

        // Process with concurrency limit (5 concurrent)
        var semaphore = new SemaphoreSlim(5, 5);
        var tasks = vins.Select(async vin =>
        {
            await semaphore.WaitAsync();
            try
            {
                var vinUpper = vin.ToUpperInvariant().Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(vinUpper, @"^[A-HJ-NPR-Z0-9]{17}$"))
                {
                    lock (errors) { errors[vin] = "Formato de VIN inválido"; }
                    return;
                }

                var result = await DecodeVinSmart(vinUpper);
                if (result.Result is OkObjectResult ok && ok.Value is SmartVinDecodeResult decoded)
                {
                    lock (results) { results.Add(decoded); }
                }
                else
                {
                    lock (errors) { errors[vin] = "No se pudo decodificar"; }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch decode failed for VIN: {VIN}", vin);
                lock (errors) { errors[vin] = ex.Message; }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return Ok(new BatchVinDecodeResponse
        {
            Results = results,
            Errors = errors,
            TotalRequested = vins.Count,
            TotalDecoded = results.Count,
            TotalFailed = errors.Count
        });
    }

    // ========================================
    // VIN HELPER METHODS
    // ========================================

    private static bool ValidateVinChecksum(string vin)
    {
        if (vin.Length != 17) return false;

        var transliteration = new Dictionary<char, int>
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
        var weights = new[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

        int sum = 0;
        for (int i = 0; i < 17; i++)
        {
            int value;
            if (char.IsDigit(vin[i]))
                value = vin[i] - '0';
            else if (transliteration.TryGetValue(vin[i], out var mapped))
                value = mapped;
            else
                return false;

            sum += value * weights[i];
        }

        int remainder = sum % 11;
        char expected = remainder == 10 ? 'X' : (char)('0' + remainder);
        return vin[8] == expected;
    }

    private static string GenerateVehicleDescription(int year, string make, string model,
        string? trim, string? engineSize, int cylinders, int horsepower,
        string fuelType, string transmission, string driveType)
    {
        var parts = new List<string>();

        // Title line
        var title = $"{year} {make} {model}";
        if (!string.IsNullOrWhiteSpace(trim)) title += $" {trim}";
        parts.Add(title);

        // Engine info
        var engineParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(engineSize)) engineParts.Add($"Motor {engineSize}");
        if (cylinders > 0) engineParts.Add($"{cylinders} cilindros");
        if (horsepower > 0) engineParts.Add($"{horsepower} HP");
        if (engineParts.Any()) parts.Add(string.Join(", ", engineParts) + ".");

        // Transmission & drive
        var transParts = new List<string>();
        if (!string.IsNullOrEmpty(transmission)) transParts.Add($"Transmisión {MapTransmissionSpanish(transmission)}");
        if (!string.IsNullOrEmpty(driveType)) transParts.Add($"tracción {MapDriveTypeSpanish(driveType)}");
        if (transParts.Any()) parts.Add(string.Join(", ", transParts) + ".");

        // Fuel
        if (!string.IsNullOrEmpty(fuelType))
            parts.Add($"Combustible: {MapFuelTypeSpanish(fuelType)}.");

        parts.Add("Ubicado en República Dominicana.");

        return string.Join(" ", parts);
    }

    private static string MapTransmissionSpanish(string t) => t switch
    {
        "automatic" => "automática",
        "manual" => "manual",
        "cvt" => "CVT",
        "dct" => "doble embrague (DCT)",
        "semi-automatic" => "semi-automática",
        // Legacy PascalCase keys (backwards compat)
        "Automatic" => "automática",
        "Manual" => "manual",
        "CVT" => "CVT",
        "DualClutch" => "doble embrague",
        "Automated" => "automatizada",
        _ => t.ToLowerInvariant()
    };

    private static string MapDriveTypeSpanish(string d) => d switch
    {
        "FWD" => "delantera (FWD)",
        "RWD" => "trasera (RWD)",
        "AWD" => "total (AWD)",
        "FourWD" => "4x4",
        _ => d
    };

    private static string MapFuelTypeSpanish(string f) => f switch
    {
        "gasoline" => "gasolina",
        "diesel" => "diésel",
        "hybrid" => "híbrido",
        "electric" => "eléctrico",
        "plugin_hybrid" => "híbrido enchufable",
        "flex_fuel" => "flex fuel",
        "lpg" => "GLP / gas",
        // Legacy PascalCase keys (backwards compat)
        "Gasoline" => "gasolina",
        "Diesel" => "diésel",
        "Hybrid" => "híbrido",
        "Electric" => "eléctrico",
        "PlugInHybrid" => "híbrido enchufable",
        "FlexFuel" => "flex fuel",
        "Hydrogen" => "hidrógeno",
        "NaturalGas" => "gas natural",
        _ => f.ToLowerInvariant()
    };

    // VIN Decode helper methods
    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value, out var result))
            return result;
        return null;
    }

    private static string MapFuelType(string? nhtsaFuelType)
    {
        if (string.IsNullOrEmpty(nhtsaFuelType)) return "gasoline";

        return nhtsaFuelType.ToLowerInvariant() switch
        {
            // Check plug-in hybrid first (before generic hybrid check)
            var f when f.Contains("plug") || f.Contains("phev") => "plugin_hybrid",
            var f when f.Contains("diesel") => "diesel",
            var f when f.Contains("hybrid") => "hybrid",
            var f when f.Contains("electric") => "electric",
            var f when f.Contains("flex") => "flex_fuel",
            var f when f.Contains("hydrogen") => "lpg",
            var f when f.Contains("natural gas") || f.Contains("cng") || f.Contains("lpg") || f.Contains("propane") => "lpg",
            _ => "gasoline"
        };
    }

    private static string MapTransmission(string? nhtsaTransmission)
    {
        if (string.IsNullOrEmpty(nhtsaTransmission)) return "automatic";

        return nhtsaTransmission.ToLowerInvariant() switch
        {
            var t when t.Contains("cvt") || t.Contains("continuously variable") => "cvt",
            var t when t.Contains("dual") || t.Contains("dct") || t.Contains("dsg") => "dct",
            var t when t.Contains("automated") || t.Contains("semi-auto") || t.Contains("semi_auto") => "semi-automatic",
            var t when t.Contains("manual") => "manual",
            _ => "automatic"
        };
    }

    private static string MapDriveType(string? nhtsaDriveType)
    {
        if (string.IsNullOrEmpty(nhtsaDriveType)) return "FWD";

        // Drive type is displayed as a text field in the frontend, so we keep
        // human-readable short forms (FWD, RWD, AWD, 4WD) instead of catalog keys.
        return nhtsaDriveType.ToLowerInvariant() switch
        {
            var d when d.Contains("4x4") || d.Contains("4wd") || d.Contains("four") => "4WD",
            var d when d.Contains("awd") || d.Contains("all") => "AWD",
            var d when d.Contains("rwd") || d.Contains("rear") => "RWD",
            _ => "FWD"
        };
    }

    /// <summary>Converts a display name to a URL-friendly slug.</summary>
    private static string ToSlug(string name)
        => System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');

    /// <summary>Parses the mapped vehicle-type string back to the domain enum.</summary>
    private static VehicleType ParseVehicleTypeEnum(string vehicleTypeStr) => vehicleTypeStr switch
    {
        "Truck" => VehicleType.Truck,
        "SUV" => VehicleType.SUV,
        "Van" => VehicleType.Van,
        "Motorcycle" => VehicleType.Motorcycle,
        "Commercial" => VehicleType.Commercial,
        _ => VehicleType.Car
    };

    /// <summary>Parses the mapped body-style string back to the domain enum (nullable).</summary>
    private static BodyStyle? ParseBodyStyleEnum(string bodyStyleStr) => bodyStyleStr switch
    {
        // New lowercase catalog keys
        "suv" => BodyStyle.SUV,
        "pickup" => BodyStyle.Pickup,
        "minivan" => BodyStyle.Minivan,
        "van" => BodyStyle.Van,
        "coupe" => BodyStyle.Coupe,
        "convertible" => BodyStyle.Convertible,
        "hatchback" => BodyStyle.Hatchback,
        "wagon" => BodyStyle.Wagon,
        "crossover" => BodyStyle.Crossover,
        "sedan" => BodyStyle.Sedan,
        // Legacy PascalCase keys (backwards compat)
        "SUV" => BodyStyle.SUV,
        "Pickup" => BodyStyle.Pickup,
        "Minivan" => BodyStyle.Minivan,
        "Van" => BodyStyle.Van,
        "Coupe" => BodyStyle.Coupe,
        "Convertible" => BodyStyle.Convertible,
        "Hatchback" => BodyStyle.Hatchback,
        "Wagon" => BodyStyle.Wagon,
        "Crossover" => BodyStyle.Crossover,
        "Sedan" => BodyStyle.Sedan,
        _ => null
    };

    private static string MapBodyStyle(string? nhtsaBodyClass)
    {
        if (string.IsNullOrEmpty(nhtsaBodyClass)) return "sedan";

        return nhtsaBodyClass.ToLowerInvariant() switch
        {
            var b when b.Contains("suv") || b.Contains("sport utility") || b.Contains("multipurpose") => "suv",
            var b when b.Contains("pickup") || b.Contains("truck") => "pickup",
            var b when b.Contains("van") && b.Contains("mini") => "minivan",
            var b when b.Contains("van") => "van",
            var b when b.Contains("coupe") => "coupe",
            var b when b.Contains("convertible") || b.Contains("cabriolet") || b.Contains("roadster") => "convertible",
            var b when b.Contains("hatchback") => "hatchback",
            var b when b.Contains("wagon") || b.Contains("estate") => "wagon",
            var b when b.Contains("crossover") => "crossover",
            _ => "sedan"
        };
    }

    private static string MapVehicleType(string? nhtsaVehicleType)
    {
        if (string.IsNullOrEmpty(nhtsaVehicleType)) return "Car";

        return nhtsaVehicleType.ToLowerInvariant() switch
        {
            var v when v.Contains("truck") => "Truck",
            var v when v.Contains("suv") || v.Contains("multipurpose") => "SUV",
            var v when v.Contains("van") => "Van",
            var v when v.Contains("motorcycle") => "Motorcycle",
            var v when v.Contains("bus") => "Commercial",
            _ => "Car"
        };
    }

    // ========================================
    // STATS & HEALTH
    // ========================================

    /// <summary>
    /// Obtiene estadísticas del catálogo.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CatalogStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogStats>> GetStats()
    {
        var stats = await _catalogRepository.GetStatsAsync();
        return Ok(stats);
    }

    // ========================================
    // CATALOG ENUMS (Tipos estáticos)
    // ========================================

    /// <summary>
    /// Obtiene los tipos de carrocería disponibles.
    /// </summary>
    [HttpGet("body-types")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetBodyTypes()
    {
        var bodyTypes = new List<CatalogOptionDto>
        {
            new("sedan", "Sedán"),
            new("suv", "SUV"),
            new("pickup", "Pickup"),
            new("hatchback", "Hatchback"),
            new("coupe", "Coupé"),
            new("convertible", "Convertible"),
            new("van", "Van"),
            new("minivan", "Minivan"),
            new("wagon", "Wagon"),
            new("crossover", "Crossover"),
            new("truck", "Camión"),
        };
        return Ok(bodyTypes);
    }

    /// <summary>
    /// Obtiene los tipos de combustible disponibles.
    /// </summary>
    [HttpGet("fuel-types")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetFuelTypes()
    {
        var fuelTypes = new List<CatalogOptionDto>
        {
            new("Gasoline", "Gasolina"),
            new("Diesel", "Diésel"),
            new("Hybrid", "Híbrido"),
            new("Electric", "Eléctrico"),
            new("PlugInHybrid", "Híbrido Enchufable"),
            new("NaturalGas", "GLP / Gas"),
            new("FlexFuel", "Flex Fuel"),
        };
        return Ok(fuelTypes);
    }

    /// <summary>
    /// Obtiene los tipos de transmisión disponibles.
    /// </summary>
    [HttpGet("transmissions")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetTransmissions()
    {
        var transmissions = new List<CatalogOptionDto>
        {
            new("Automatic", "Automática"),
            new("Manual", "Manual"),
            new("CVT", "CVT"),
            new("DualClutch", "Doble Embrague (DCT)"),
            new("Automated", "Semi-automática"),
        };
        return Ok(transmissions);
    }

    /// <summary>
    /// Obtiene los tipos de tracción disponibles.
    /// </summary>
    [HttpGet("drive-types")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetDriveTypes()
    {
        var driveTypes = new List<CatalogOptionDto>
        {
            new("FWD", "Delantera (FWD)"),
            new("RWD", "Trasera (RWD)"),
            new("AWD", "Total (AWD)"),
            new("FourWD", "4x4 (4WD)"),
        };
        return Ok(driveTypes);
    }

    /// <summary>
    /// Obtiene las condiciones de vehículo disponibles.
    /// </summary>
    [HttpGet("conditions")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetConditions()
    {
        var conditions = new List<CatalogOptionDto>
        {
            new("New", "Nuevo"),
            new("CertifiedPreOwned", "Certificado Pre-Owned"),
            new("Used", "Usado"),
            new("Salvage", "Salvamento"),
            new("Rebuilt", "Reconstruido"),
        };
        return Ok(conditions);
    }

    /// <summary>
    /// Obtiene los colores disponibles para vehículos.
    /// </summary>
    [HttpGet("colors")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetColors()
    {
        var colors = new List<CatalogOptionDto>
        {
            new("white", "Blanco"),
            new("black", "Negro"),
            new("silver", "Plateado"),
            new("gray", "Gris"),
            new("red", "Rojo"),
            new("blue", "Azul"),
            new("green", "Verde"),
            new("brown", "Marrón"),
            new("beige", "Beige"),
            new("gold", "Dorado"),
            new("orange", "Naranja"),
            new("yellow", "Amarillo"),
            new("burgundy", "Borgoña"),
            new("champagne", "Champán"),
            new("pearl_white", "Blanco Perla"),
        };
        return Ok(colors);
    }

    /// <summary>
    /// Obtiene las provincias de República Dominicana.
    /// </summary>
    [HttpGet("provinces")]
    [ProducesResponseType(typeof(IEnumerable<CatalogOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogOptionDto>> GetProvinces()
    {
        var provinces = new List<CatalogOptionDto>
        {
            new("santo-domingo", "Santo Domingo"),
            new("distrito-nacional", "Distrito Nacional"),
            new("santiago", "Santiago"),
            new("san-cristobal", "San Cristóbal"),
            new("la-vega", "La Vega"),
            new("san-pedro-de-macoris", "San Pedro de Macorís"),
            new("puerto-plata", "Puerto Plata"),
            new("la-romana", "La Romana"),
            new("la-altagracia", "La Altagracia"),
            new("duarte", "Duarte"),
            new("espaillat", "Espaillat"),
            new("azua", "Azua"),
            new("barahona", "Barahona"),
            new("monte-plata", "Monte Plata"),
            new("peravia", "Peravia"),
            new("valverde", "Valverde"),
            new("san-juan", "San Juan"),
            new("monsenor-nouel", "Monseñor Nouel"),
            new("maria-trinidad-sanchez", "María Trinidad Sánchez"),
            new("samana", "Samaná"),
            new("sanchez-ramirez", "Sánchez Ramírez"),
            new("monte-cristi", "Monte Cristi"),
            new("hato-mayor", "Hato Mayor"),
            new("dajabon", "Dajabón"),
            new("el-seibo", "El Seibo"),
            new("elias-pina", "Elías Piña"),
            new("hermanas-mirabal", "Hermanas Mirabal"),
            new("independencia", "Independencia"),
            new("baoruco", "Baoruco"),
            new("pedernales", "Pedernales"),
            new("santiago-rodriguez", "Santiago Rodríguez"),
        };
        return Ok(provinces);
    }

    // ========================================
    // SEED / IMPORT
    // ========================================

    /// <summary>
    /// Importa catálogo de vehículos masivamente.
    /// Este endpoint recibe datos de la API NHTSA procesados y los inserta en la BD.
    /// Solo agrega datos nuevos (upsert - no duplica).
    /// </summary>
    /// <remarks>
    /// Usar con el script: scripts/seed-vehicle-catalog.ts
    /// 
    /// Ejemplo payload:
    /// {
    ///   "makes": [{
    ///     "name": "Toyota",
    ///     "slug": "toyota",
    ///     "country": "Japan",
    ///     "isPopular": true,
    ///     "models": [{
    ///       "name": "Camry",
    ///       "vehicleType": "Car",
    ///       "bodyStyle": "Sedan",
    ///       "trims": [{
    ///         "name": "LE",
    ///         "year": 2024,
    ///         "baseMSRP": 28400,
    ///         "horsepower": 203,
    ///         ...
    ///       }]
    ///     }]
    ///   }]
    /// }
    /// </remarks>
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SeedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SeedResult>> SeedCatalog([FromBody] SeedCatalogRequest request)
    {
        if (request.Makes == null || !request.Makes.Any())
        {
            return BadRequest(new { message = "No makes provided" });
        }

        _logger.LogInformation("Starting catalog seed with {Count} makes", request.Makes.Count);

        int makesCount = 0;
        int modelsCount = 0;
        int trimsCount = 0;

        foreach (var makeDto in request.Makes)
        {
            // Upsert Make
            var make = new VehicleMake
            {
                Name = makeDto.Name,
                Slug = makeDto.Slug ?? CreateSlug(makeDto.Name),
                Country = makeDto.Country,
                IsPopular = makeDto.IsPopular,
                IsActive = true
            };

            var savedMake = await _catalogRepository.UpsertMakeAsync(make);
            makesCount++;

            foreach (var modelDto in makeDto.Models ?? Enumerable.Empty<SeedModelDto>())
            {
                // Parse VehicleType
                if (!Enum.TryParse<VehicleType>(modelDto.VehicleType, true, out var vehicleType))
                {
                    vehicleType = VehicleType.Car;
                }

                // Parse BodyStyle
                BodyStyle? bodyStyle = null;
                if (!string.IsNullOrEmpty(modelDto.BodyStyle) &&
                    Enum.TryParse<BodyStyle>(modelDto.BodyStyle, true, out var bs))
                {
                    bodyStyle = bs;
                }

                var model = new VehicleModel
                {
                    MakeId = savedMake.Id,
                    Name = modelDto.Name,
                    Slug = modelDto.Slug ?? CreateSlug(modelDto.Name),
                    VehicleType = vehicleType,
                    DefaultBodyStyle = bodyStyle,
                    StartYear = modelDto.StartYear,
                    EndYear = modelDto.EndYear,
                    IsPopular = modelDto.IsPopular,
                    IsActive = true
                };

                var savedModel = await _catalogRepository.UpsertModelAsync(model);
                modelsCount++;

                foreach (var trimDto in modelDto.Trims ?? Enumerable.Empty<SeedTrimDto>())
                {
                    // Parse enums
                    FuelType? fuelType = null;
                    if (!string.IsNullOrEmpty(trimDto.FuelType) &&
                        Enum.TryParse<FuelType>(trimDto.FuelType, true, out var ft))
                    {
                        fuelType = ft;
                    }

                    TransmissionType? transmission = null;
                    if (!string.IsNullOrEmpty(trimDto.Transmission) &&
                        Enum.TryParse<TransmissionType>(trimDto.Transmission, true, out var tt))
                    {
                        transmission = tt;
                    }

                    VehiclesSaleService.Domain.Entities.DriveType? driveType = null;
                    if (!string.IsNullOrEmpty(trimDto.DriveType) &&
                        Enum.TryParse<VehiclesSaleService.Domain.Entities.DriveType>(trimDto.DriveType, true, out var dt))
                    {
                        driveType = dt;
                    }

                    var trim = new VehicleTrim
                    {
                        ModelId = savedModel.Id,
                        Name = trimDto.Name,
                        Slug = trimDto.Slug ?? CreateSlug($"{modelDto.Name}-{trimDto.Name}-{trimDto.Year}"),
                        Year = trimDto.Year,
                        BaseMSRP = trimDto.BaseMSRP,
                        EngineSize = trimDto.EngineSize,
                        Horsepower = trimDto.Horsepower,
                        Torque = trimDto.Torque,
                        FuelType = fuelType,
                        Transmission = transmission,
                        DriveType = driveType,
                        MpgCity = trimDto.MpgCity,
                        MpgHighway = trimDto.MpgHighway,
                        MpgCombined = trimDto.MpgCombined,
                        IsActive = true
                    };

                    await _catalogRepository.UpsertTrimAsync(trim);
                    trimsCount++;
                }
            }
        }

        _logger.LogInformation("Catalog seed completed: {Makes} makes, {Models} models, {Trims} trims",
            makesCount, modelsCount, trimsCount);

        return Ok(new SeedResult
        {
            Success = true,
            MakesCount = makesCount,
            ModelsCount = modelsCount,
            TrimsCount = trimsCount,
            Message = $"Successfully imported {makesCount} makes, {modelsCount} models, {trimsCount} trims"
        });
    }

    private static string CreateSlug(string text)
    {
        return text
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace(".", "")
            .Replace(",", "");
    }
}

// ========================================
// SEED DTOs
// ========================================

public record SeedCatalogRequest
{
    public List<SeedMakeDto>? Makes { get; init; }
}

public record SeedMakeDto
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Country { get; init; }
    public bool IsPopular { get; init; }
    public List<SeedModelDto>? Models { get; init; }
}

public record SeedModelDto
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string VehicleType { get; init; } = "Car";
    public string? BodyStyle { get; init; }
    public int? StartYear { get; init; }
    public int? EndYear { get; init; }
    public bool IsPopular { get; init; }
    public List<SeedTrimDto>? Trims { get; init; }
}

public record SeedTrimDto
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public int Year { get; init; }
    public decimal? BaseMSRP { get; init; }
    public string? EngineSize { get; init; }
    public int? Horsepower { get; init; }
    public int? Torque { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? DriveType { get; init; }
    public int? MpgCity { get; init; }
    public int? MpgHighway { get; init; }
    public int? MpgCombined { get; init; }
}

public record SeedResult
{
    public bool Success { get; init; }
    public int MakesCount { get; init; }
    public int ModelsCount { get; init; }
    public int TrimsCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

// ========================================
// DTOs
// ========================================

public record MakeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Country { get; init; }
    public bool IsPopular { get; init; }
}

public record ModelDto
{
    public Guid Id { get; init; }
    public Guid MakeId { get; init; }
    public string MakeName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string VehicleType { get; init; } = string.Empty;
    public string? BodyStyle { get; init; }
    public int? StartYear { get; init; }
    public int? EndYear { get; init; }
    public bool IsPopular { get; init; }
}

public record TrimDto
{
    public Guid Id { get; init; }
    public Guid ModelId { get; init; }
    public string MakeName { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int Year { get; init; }

    // Engine & Performance - Auto-fill fields
    public string? EngineSize { get; init; }
    public int? Horsepower { get; init; }
    public int? Torque { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? DriveType { get; init; }

    // Fuel Economy - Auto-fill fields
    public int? MpgCity { get; init; }
    public int? MpgHighway { get; init; }
    public int? MpgCombined { get; init; }

    // Pricing reference
    public decimal? BaseMSRP { get; init; }
}

// ========================================
// VIN DECODE DTOs
// ========================================

/// <summary>
/// Response from VIN decoding
/// </summary>
public record VinDecodeResponse
{
    public string VIN { get; init; } = string.Empty;
    public bool IsValid { get; init; }

    // Basic Info
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Trim { get; init; }

    // Type & Body
    public string VehicleType { get; init; } = "Car";
    public string BodyStyle { get; init; } = "Sedan";
    public int? Doors { get; init; }

    // Engine
    public string? EngineSize { get; init; }
    public int? Cylinders { get; init; }
    public int? Horsepower { get; init; }
    public string FuelType { get; init; } = "Gasoline";

    // Transmission & Drive
    public string Transmission { get; init; } = "Automatic";
    public string DriveType { get; init; } = "FWD";

    // Manufacturing
    public string? PlantCity { get; init; }
    public string? PlantCountry { get; init; }
    public string? Manufacturer { get; init; }

    // Additional
    public string? Series { get; init; }
    public string? GVWR { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    // Suggested data for form auto-fill
    public VinSuggestedData? SuggestedData { get; init; }
}

/// <summary>
/// Suggested values for form auto-fill based on VIN
/// </summary>
public record VinSuggestedData
{
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Trim { get; init; }
    public string VehicleType { get; init; } = "Car";
    public string BodyStyle { get; init; } = "Sedan";
    public string FuelType { get; init; } = "Gasoline";
    public string Transmission { get; init; } = "Automatic";
    public string DriveType { get; init; } = "FWD";
    public string? EngineSize { get; init; }
    public int? Horsepower { get; init; }
    public int? Cylinders { get; init; }
}

/// <summary>
/// NHTSA VPIC API Response
/// </summary>
public class NhtsaVinResponse
{
    public int Count { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SearchCriteria { get; set; } = string.Empty;
    public List<NhtsaVinResult> Results { get; set; } = new();
}

/// <summary>
/// NHTSA VPIC Result item
/// </summary>
public class NhtsaVinResult
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? ModelYear { get; set; }
    public string? Trim { get; set; }
    public string? Series { get; set; }
    public string? VehicleType { get; set; }
    public string? BodyClass { get; set; }
    public string? Doors { get; set; }
    public string? DriveType { get; set; }
    public string? TransmissionStyle { get; set; }
    public string? FuelTypePrimary { get; set; }
    public string? DisplacementL { get; set; }
    public string? EngineModel { get; set; }
    public string? EngineCylinders { get; set; }
    public string? EngineHP { get; set; }
    public string? Manufacturer { get; set; }
    public string? PlantCity { get; set; }
    public string? PlantCountry { get; set; }
    public string? GVWR { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorText { get; set; }
}

/// <summary>
/// DTO genérico para opciones del catálogo (body-types, fuel-types, etc.)
/// IMPORTANT: Property names must be "Value" and "Label" to match the frontend
/// CatalogOption interface { value: string; label: string }.
/// </summary>
public record CatalogOptionDto(string Value, string Label);

// ========================================
// SMART VIN DECODE DTOs
// ========================================

/// <summary>
/// Enriched VIN decode result with catalog matching and duplicate detection.
/// </summary>
public record SmartVinDecodeResult
{
    public string VIN { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int? Year { get; init; }
    public string? Trim { get; init; }
    public string? BodyStyle { get; init; }
    public string? VehicleType { get; init; }
    public string? EngineSize { get; init; }
    public int? Cylinders { get; init; }
    public int? Horsepower { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? DriveType { get; init; }
    public int? Doors { get; init; }
    public string? ManufacturedIn { get; init; }
    public string? PlantCountry { get; init; }

    // Catalog match
    public Guid? CatalogMakeId { get; init; }
    public Guid? CatalogModelId { get; init; }
    public Guid? CatalogTrimId { get; init; }
    public bool HasCatalogMatch { get; init; }

    // Duplicate check
    public bool IsDuplicate { get; init; }
    public Guid? ExistingVehicleId { get; init; }
    public string? ExistingVehicleSlug { get; init; }

    // Quality & confidence
    public Dictionary<string, FieldConfidence> FieldConfidences { get; init; } = new();
    public string? SuggestedDescription { get; init; }

    // Auto-fill data for frontend form
    public VinSuggestedData? AutoFill { get; init; }
}

/// <summary>
/// Confidence level for a decoded field.
/// </summary>
public record FieldConfidence(string Value, string Source, double Confidence);

/// <summary>
/// Batch VIN decode request.
/// </summary>
public record BatchVinDecodeRequest
{
    public List<string> Vins { get; init; } = new();
    public int? MaxItems { get; init; } = 50;
}

/// <summary>
/// Batch VIN decode response.
/// </summary>
public record BatchVinDecodeResponse
{
    public List<SmartVinDecodeResult> Results { get; init; } = new();
    public Dictionary<string, string> Errors { get; init; } = new();
    public int TotalRequested { get; init; }
    public int TotalDecoded { get; init; }
    public int TotalFailed { get; init; }
}

/// <summary>
/// VIN existence check response.
/// </summary>
public record VinExistsResponse
{
    public bool Exists { get; init; }
    public Guid? VehicleId { get; init; }
    public string? Slug { get; init; }
    public string? Status { get; init; }
}
