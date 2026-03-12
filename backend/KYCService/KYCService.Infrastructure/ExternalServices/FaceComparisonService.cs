using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KYCService.Infrastructure.ExternalServices;

/// <summary>
/// Configuración del servicio de comparación facial
/// </summary>
public class FaceComparisonConfig
{
    public const string SectionName = "FaceComparison";

    /// <summary>
    /// Usar modo simulación (para desarrollo y tests)
    /// </summary>
    public bool UseSimulation { get; set; } = true;

    /// <summary>
    /// Usar Amazon Rekognition (RECOMENDADO - económico ~$0.001/imagen)
    /// </summary>
    public bool UseAmazonRekognition { get; set; } = false;

    /// <summary>
    /// Usar Azure Face API (más caro, solo como alternativa)
    /// </summary>
    public bool UseAzureFaceApi { get; set; } = false;

    /// <summary>
    /// Endpoint de Azure Face API (si se usa)
    /// </summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>
    /// API Key de Azure Face API (si se usa)
    /// </summary>
    public string? AzureApiKey { get; set; }

    /// <summary>
    /// Umbral de similitud para considerar match (0-100)
    /// </summary>
    public float MatchThreshold { get; set; } = 80;

    /// <summary>
    /// Umbral de liveness (0-100)
    /// </summary>
    public float LivenessThreshold { get; set; } = 70;

    /// <summary>
    /// Número mínimo de challenges requeridos para liveness
    /// </summary>
    public int MinimumLivenessChallenges { get; set; } = 2;

    /// <summary>
    /// Tiempo máximo de procesamiento en segundos
    /// </summary>
    public int MaxProcessingTimeSeconds { get; set; } = 30;
}

/// <summary>
/// Implementación del servicio de comparación facial.
/// 
/// Modos de operación (en orden de prioridad):
/// 1. Simulación - Para desarrollo y tests
/// 2. Amazon Rekognition - RECOMENDADO (~$0.001/imagen, muy económico)
/// 3. Azure Face API - Cloud, más caro (solo como alternativa)
/// 
/// RECOMENDADO: Usar Amazon Rekognition para producción.
/// Costo estimado: ~$2-4 USD/mes para 1000 verificaciones KYC
/// </summary>
public class FaceComparisonService : IFaceComparisonService, IDisposable
{
    private readonly ILogger<FaceComparisonService> _logger;
    private readonly FaceComparisonConfig _config;
    private readonly AmazonRekognitionService? _rekognitionService;
    private bool _disposed;

    public FaceComparisonService(
        ILogger<FaceComparisonService> logger,
        IOptions<FaceComparisonConfig> config,
        AmazonRekognitionService? rekognitionService = null)
    {
        _logger = logger;
        _config = config.Value;
        _rekognitionService = rekognitionService;

        var mode = _config.UseSimulation ? "Simulation"
            : (_config.UseAmazonRekognition ? "Amazon Rekognition"
            : (_config.UseAzureFaceApi ? "Azure Face API" : "Unknown"));

        _logger.LogInformation("Face Comparison Service initialized. Mode: {Mode}", mode);
    }

    /// <inheritdoc />
    public async Task<FaceComparisonResult> CompareFacesAsync(
        byte[] image1,
        byte[] image2,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new FaceComparisonResult { Threshold = (decimal)_config.MatchThreshold };

        try
        {
            _logger.LogInformation("Starting face comparison...");

            if (_config.UseSimulation)
            {
                result = await SimulateComparisonAsync(cancellationToken);
            }
            else if (_config.UseAmazonRekognition && _rekognitionService != null)
            {
                // RECOMENDADO: Usar Amazon Rekognition (económico y preciso)
                var rekognitionResult = await _rekognitionService.CompareFacesAsync(image1, image2, cancellationToken);

                result.Success = rekognitionResult.Success;
                result.IsMatch = rekognitionResult.IsMatch;
                result.SimilarityScore = rekognitionResult.SimilarityScore;
                result.Confidence = rekognitionResult.Confidence;
                result.Threshold = rekognitionResult.Threshold;
                result.ErrorMessage = rekognitionResult.ErrorMessage;
                result.Details = rekognitionResult.Details;
            }
            else if (_config.UseAzureFaceApi)
            {
                result = CompareWithAzureFaceApi(image1, image2);
            }
            else
            {
                _logger.LogWarning("No face comparison service configured");
                result.Success = false;
                result.ErrorMessage = "Servicio de comparación facial no configurado";
                return result;
            }

            result.IsMatch = result.SimilarityScore >= result.Threshold;

            _logger.LogInformation("Face comparison completed. Match: {IsMatch}, Score: {Score}%",
                result.IsMatch, result.SimilarityScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face comparison failed");
            result.Success = false;
            result.ErrorMessage = "Error comparando rostros. Intente nuevamente.";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<FaceComparisonResult> CompareWithDocumentAsync(
        byte[] selfieImage,
        byte[] documentImage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing selfie with document photo...");

        // Para Amazon Rekognition, comparamos directamente las imágenes
        // El servicio automáticamente detecta el rostro en cada imagen
        return await CompareFacesAsync(selfieImage, documentImage, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FaceDetectionResult> DetectFacesAsync(
        byte[] imageData,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new FaceDetectionResult();

        try
        {
            _logger.LogDebug("Detecting faces in image ({Size} bytes)", imageData.Length);

            if (_config.UseSimulation)
            {
                result = SimulateFaceDetection();
            }
            else if (_config.UseAmazonRekognition && _rekognitionService != null)
            {
                result = await _rekognitionService.DetectFacesAsync(imageData, cancellationToken);
            }
            else if (_config.UseAzureFaceApi)
            {
                result = DetectWithAzureFaceApi(imageData);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Servicio de detección facial no configurado";
            }

            _logger.LogInformation("Face detection completed. Faces found: {Count}", result.FaceCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face detection failed");
            result.Success = false;
            result.ErrorMessage = "Error detectando rostros.";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<LivenessResult> CheckLivenessAsync(
        LivenessCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new LivenessResult();

        try
        {
            _logger.LogDebug("Checking liveness...");

            if (_config.UseSimulation)
            {
                result = SimulateLivenessCheck(request);
            }
            else if (_config.UseAmazonRekognition && _rekognitionService != null)
            {
                // Determinar el challenge basado en los resultados del request
                var challengeType = request.ChallengeResults.FirstOrDefault()?.ChallengeType ?? "None";
                var challenge = Enum.TryParse<LivenessChallenge>(challengeType, true, out var c)
                    ? c : LivenessChallenge.None;
                result = await _rekognitionService.CheckBasicLivenessAsync(request.SelfieImage, challenge, cancellationToken);
            }
            else
            {
                // Liveness básico - verificar que la imagen tiene un rostro válido
                var detection = await DetectFacesAsync(request.SelfieImage, cancellationToken);
                result.Success = detection.Success;
                result.IsLive = detection.FaceCount == 1 && detection.Faces.Any(f => f.Confidence > 90);
                result.LivenessScore = detection.Faces.FirstOrDefault()?.Confidence ?? 0;
                result.Threshold = (decimal)_config.LivenessThreshold;
            }

            _logger.LogInformation("Liveness check completed. IsLive: {IsLive}, Score: {Score}%",
                result.IsLive, result.LivenessScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness check failed");
            result.Success = false;
            result.ErrorMessage = "Error verificando liveness.";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<FaceExtractionResult> ExtractFaceFromDocumentAsync(
        byte[] documentImage,
        CancellationToken cancellationToken = default)
    {
        var result = new FaceExtractionResult();

        try
        {
            _logger.LogDebug("Extracting face from document ({Size} bytes)", documentImage.Length);

            if (_config.UseSimulation)
            {
                result = SimulateFaceExtraction(documentImage);
            }
            else if (_config.UseAmazonRekognition && _rekognitionService != null)
            {
                var extraction = await _rekognitionService.ExtractFaceFromDocumentAsync(documentImage, cancellationToken);
                result.Success = extraction.Success;
                result.BoundingBox = extraction.BoundingBox;
                result.Quality = extraction.Quality;
                result.ErrorMessage = extraction.ErrorMessage;

                // Retornar la imagen completa - en producción se podría recortar
                if (extraction.Success)
                {
                    result.FaceImage = documentImage;
                }
            }
            else if (_config.UseAzureFaceApi)
            {
                result = ExtractWithAzureFaceApi(documentImage);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Servicio de extracción facial no configurado";
            }

            _logger.LogInformation("Face extraction completed. Success: {Success}", result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face extraction failed");
            result.Success = false;
            result.ErrorMessage = "Error extrayendo rostro del documento.";
        }

        return result;
    }

    #region Simulación (Desarrollo y Testing)

    private Task<FaceComparisonResult> SimulateComparisonAsync(CancellationToken ct)
    {
        // Simular delay de procesamiento
        var random = new Random();
        var score = random.Next(75, 100);

        return Task.FromResult(new FaceComparisonResult
        {
            Success = true,
            IsMatch = score >= _config.MatchThreshold,
            SimilarityScore = score,
            Confidence = score,
            Threshold = (decimal)_config.MatchThreshold,
            ErrorMessage = null
        });
    }

    private FaceDetectionResult SimulateFaceDetection()
    {
        var random = new Random();
        return new FaceDetectionResult
        {
            Success = true,
            FaceCount = 1,
            Faces = new List<DetectedFace>
            {
                new DetectedFace
                {
                    FaceId = Guid.NewGuid().ToString(),
                    BoundingBox = new FaceBoundingBox { X = 100, Y = 100, Width = 200, Height = 250 },
                    Confidence = random.Next(85, 99),
                    EstimatedAge = random.Next(25, 50),
                    DetectedGender = random.Next(2) == 0 ? "Male" : "Female",
                    EyesOpen = true,
                    Smile = false
                }
            },
            ProcessingTimeMs = random.Next(100, 500)
        };
    }

    private LivenessResult SimulateLivenessCheck(LivenessCheckRequest request)
    {
        var random = new Random();
        var passed = random.Next(100) > 15; // 85% éxito
        var challengeType = request.ChallengeResults.FirstOrDefault()?.ChallengeType ?? "None";

        return new LivenessResult
        {
            Success = true,
            IsLive = passed,
            LivenessScore = passed ? random.Next(75, 99) : random.Next(20, 50),
            Threshold = (decimal)_config.LivenessThreshold,
            ChallengeDetails = new List<LivenessChallengeDetail>
            {
                new LivenessChallengeDetail
                {
                    ChallengeType = challengeType,
                    Passed = passed,
                    Score = passed ? random.Next(75, 99) : random.Next(20, 50),
                    Notes = passed ? "[SIMULACIÓN] Liveness verificado" : "[SIMULACIÓN] Liveness fallido"
                }
            },
            FailureReason = passed ? null : "Simulación de fallo"
        };
    }

    private FaceExtractionResult SimulateFaceExtraction(byte[] documentImage)
    {
        return new FaceExtractionResult
        {
            Success = true,
            FaceImage = documentImage, // En simulación, retornamos la misma imagen
            BoundingBox = new FaceBoundingBox { X = 50, Y = 80, Width = 150, Height = 180 },
            Quality = 92.5m,
            ErrorMessage = null
        };
    }

    #endregion

    #region Azure Face API (Fallback - No implementado completamente)

    private FaceComparisonResult CompareWithAzureFaceApi(byte[] image1, byte[] image2)
    {
        _logger.LogWarning("Azure Face API not implemented. Please use Amazon Rekognition instead.");

        return new FaceComparisonResult
        {
            Success = false,
            ErrorMessage = "Azure Face API no implementado. Configure Amazon Rekognition."
        };
    }

    private FaceDetectionResult DetectWithAzureFaceApi(byte[] imageData)
    {
        _logger.LogWarning("Azure Face API detection not implemented.");

        return new FaceDetectionResult
        {
            Success = false,
            ErrorMessage = "Azure Face API no implementado. Configure Amazon Rekognition."
        };
    }

    private FaceExtractionResult ExtractWithAzureFaceApi(byte[] documentImage)
    {
        _logger.LogWarning("Azure Face API extraction not implemented.");

        return new FaceExtractionResult
        {
            Success = false,
            ErrorMessage = "Azure Face API no implementado. Configure Amazon Rekognition."
        };
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _rekognitionService?.Dispose();
            _disposed = true;
        }
    }
}
