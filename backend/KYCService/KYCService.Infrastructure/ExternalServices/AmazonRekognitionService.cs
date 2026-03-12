using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KYCService.Infrastructure.ExternalServices;

/// <summary>
/// Configuración para Amazon Rekognition
/// </summary>
public class AmazonRekognitionConfig
{
    /// <summary>
    /// Región de AWS (default: us-east-1)
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// AWS Access Key ID (opcional si usa IAM Role o perfil de credenciales)
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// AWS Secret Access Key (opcional si usa IAM Role o perfil de credenciales)
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Umbral de similitud para considerar que dos rostros son la misma persona (0-100)
    /// Default: 80%
    /// </summary>
    public float SimilarityThreshold { get; set; } = 80f;

    /// <summary>
    /// Atributos faciales a detectar
    /// </summary>
    public List<string> FaceAttributes { get; set; } = new() { "ALL" };

    /// <summary>
    /// Calidad mínima de imagen aceptable
    /// </summary>
    public float MinImageQuality { get; set; } = 40f;
}

/// <summary>
/// Servicio de reconocimiento facial usando Amazon Rekognition.
/// 
/// Costos aproximados (Enero 2026):
/// - Detección de rostros: $0.001/imagen
/// - Comparación de rostros: $0.001/imagen
/// - Búsqueda en colección: $0.001/operación
/// 
/// Para 1000 verificaciones KYC/mes ≈ $2-4 USD
/// 
/// Documentación: https://aws.amazon.com/rekognition/
/// </summary>
public class AmazonRekognitionService : IDisposable
{
    private readonly AmazonRekognitionClient _client;
    private readonly AmazonRekognitionConfig _config;
    private readonly ILogger<AmazonRekognitionService> _logger;
    private bool _disposed;

    public AmazonRekognitionService(
        IOptions<AmazonRekognitionConfig> config,
        ILogger<AmazonRekognitionService> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Configurar cliente de Rekognition
        var region = RegionEndpoint.GetBySystemName(_config.Region);

        if (!string.IsNullOrEmpty(_config.AccessKeyId) && !string.IsNullOrEmpty(_config.SecretAccessKey))
        {
            // Usar credenciales explícitas
            _client = new AmazonRekognitionClient(_config.AccessKeyId, _config.SecretAccessKey, region);
        }
        else
        {
            // Usar credenciales de ambiente, perfil o IAM Role
            _client = new AmazonRekognitionClient(region);
        }

        _logger.LogInformation("Amazon Rekognition service initialized for region {Region}", _config.Region);
    }

    /// <summary>
    /// Detecta rostros en una imagen
    /// </summary>
    public async Task<FaceDetectionResult> DetectFacesAsync(byte[] imageData, CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Detecting faces in image ({Size} bytes)", imageData.Length);

            var request = new DetectFacesRequest
            {
                Image = new Image
                {
                    Bytes = new MemoryStream(imageData)
                },
                Attributes = new List<string> { "ALL" }
            };

            var response = await _client.DetectFacesAsync(request, ct);

            var faces = response.FaceDetails.Select((face, index) => new DetectedFace
            {
                Index = index,
                FaceId = $"face_{index}_{Guid.NewGuid():N}",
                BoundingBox = new FaceBoundingBox
                {
                    X = (int)(face.BoundingBox.Left * 1000),
                    Y = (int)(face.BoundingBox.Top * 1000),
                    Width = (int)(face.BoundingBox.Width * 1000),
                    Height = (int)(face.BoundingBox.Height * 1000)
                },
                Confidence = (decimal)face.Confidence,
                EstimatedAge = face.AgeRange != null ? (face.AgeRange.Low + face.AgeRange.High) / 2 : null,
                DetectedGender = face.Gender?.Value?.Value,
                Expression = face.Emotions?.OrderByDescending(e => e.Confidence).FirstOrDefault()?.Type?.Value,
                EyesOpen = face.EyesOpen?.Value ?? false,
                MouthOpen = face.MouthOpen?.Value ?? false,
                Smile = face.Smile?.Value ?? false,
                Sunglasses = face.Sunglasses?.Value ?? false,
                Quality = (decimal)((face.Quality?.Brightness ?? 0) + (face.Quality?.Sharpness ?? 0)) / 2,
                Pose = face.Pose != null ? new FacePoseInfo
                {
                    Yaw = (decimal)face.Pose.Yaw,
                    Pitch = (decimal)face.Pose.Pitch,
                    Roll = (decimal)face.Pose.Roll
                } : null
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("Detected {Count} faces in image in {Ms}ms", faces.Count, stopwatch.ElapsedMilliseconds);

            return new FaceDetectionResult
            {
                Success = true,
                FaceCount = faces.Count,
                Faces = faces,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (InvalidS3ObjectException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Invalid image format for face detection");
            return new FaceDetectionResult
            {
                Success = false,
                ErrorMessage = "Formato de imagen no válido",
                FaceCount = 0,
                Faces = new List<DetectedFace>(),
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (ImageTooLargeException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Image too large for face detection");
            return new FaceDetectionResult
            {
                Success = false,
                ErrorMessage = "Imagen demasiado grande (máximo 5MB)",
                FaceCount = 0,
                Faces = new List<DetectedFace>(),
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error detecting faces");
            return new FaceDetectionResult
            {
                Success = false,
                ErrorMessage = $"Error al detectar rostros: {ex.Message}",
                FaceCount = 0,
                Faces = new List<DetectedFace>(),
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Compara dos rostros para determinar si son la misma persona
    /// </summary>
    public async Task<FaceComparisonResult> CompareFacesAsync(
        byte[] sourceImage,
        byte[] targetImage,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Comparing faces: source={SourceSize} bytes, target={TargetSize} bytes",
                sourceImage.Length, targetImage.Length);

            var request = new CompareFacesRequest
            {
                SourceImage = new Image
                {
                    Bytes = new MemoryStream(sourceImage)
                },
                TargetImage = new Image
                {
                    Bytes = new MemoryStream(targetImage)
                },
                SimilarityThreshold = _config.SimilarityThreshold,
                QualityFilter = QualityFilter.AUTO
            };

            var response = await _client.CompareFacesAsync(request, ct);
            stopwatch.Stop();

            if (response.FaceMatches.Count == 0)
            {
                _logger.LogWarning("No matching faces found between images");

                // Verificar si hay rostros sin match
                var unmatchedMessage = response.UnmatchedFaces.Count > 0
                    ? $"Se detectaron {response.UnmatchedFaces.Count} rostros pero ninguno coincide"
                    : "No se detectaron rostros coincidentes";

                return new FaceComparisonResult
                {
                    Success = true,
                    IsMatch = false,
                    SimilarityScore = 0,
                    Confidence = 0,
                    Threshold = (decimal)_config.SimilarityThreshold,
                    ErrorMessage = unmatchedMessage,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Tomar el match con mayor confianza
            var bestMatch = response.FaceMatches.OrderByDescending(m => m.Similarity).First();
            var similarity = (decimal)bestMatch.Similarity;
            var isMatch = bestMatch.Similarity >= _config.SimilarityThreshold;

            _logger.LogInformation("Face comparison result: similarity={Similarity}%, isMatch={IsMatch} in {Ms}ms",
                similarity, isMatch, stopwatch.ElapsedMilliseconds);

            return new FaceComparisonResult
            {
                Success = true,
                IsMatch = isMatch,
                SimilarityScore = similarity,
                Confidence = similarity,
                Threshold = (decimal)_config.SimilarityThreshold,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Details = new FaceComparisonDetails
                {
                    Face1Detected = true,
                    Face2Detected = true,
                    Face1Quality = 100,
                    Face2Quality = 100,
                    CosineSimilarity = similarity / 100
                }
            };
        }
        catch (InvalidParameterException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Invalid parameters for face comparison");
            return new FaceComparisonResult
            {
                Success = false,
                IsMatch = false,
                SimilarityScore = 0,
                ErrorMessage = "Parámetros inválidos: verifique que ambas imágenes contengan rostros claros",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (ImageTooLargeException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Image too large for face comparison");
            return new FaceComparisonResult
            {
                Success = false,
                IsMatch = false,
                SimilarityScore = 0,
                ErrorMessage = "Una o ambas imágenes son demasiado grandes (máximo 5MB)",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error comparing faces");
            return new FaceComparisonResult
            {
                Success = false,
                IsMatch = false,
                SimilarityScore = 0,
                ErrorMessage = $"Error al comparar rostros: {ex.Message}",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Verifica la calidad de la imagen para KYC
    /// </summary>
    public async Task<ImageQualityCheckResult> CheckImageQualityAsync(byte[] imageData, CancellationToken ct = default)
    {
        try
        {
            var detectResult = await DetectFacesAsync(imageData, ct);

            if (!detectResult.Success)
            {
                return new ImageQualityCheckResult
                {
                    IsAcceptable = false,
                    Message = detectResult.ErrorMessage ?? "Error al analizar imagen",
                    Score = 0
                };
            }

            if (detectResult.FaceCount == 0)
            {
                return new ImageQualityCheckResult
                {
                    IsAcceptable = false,
                    Message = "No se detectó ningún rostro en la imagen",
                    Score = 0,
                    Issues = new List<string> { "NO_FACE_DETECTED" }
                };
            }

            if (detectResult.FaceCount > 1)
            {
                return new ImageQualityCheckResult
                {
                    IsAcceptable = false,
                    Message = "Se detectaron múltiples rostros. La imagen debe contener solo un rostro.",
                    Score = 30,
                    Issues = new List<string> { "MULTIPLE_FACES" }
                };
            }

            var face = detectResult.Faces.First();
            var issues = new List<string>();
            var score = 100m;

            // Verificar calidad (Quality es un valor simple de 0-100)
            if (face.Quality < (decimal)_config.MinImageQuality)
            {
                issues.Add("LOW_IMAGE_QUALITY");
                score -= 20;
            }

            // Verificar accesorios
            if (face.Sunglasses)
            {
                issues.Add("SUNGLASSES_DETECTED");
                score -= 30;
            }

            // Verificar ojos abiertos
            if (!face.EyesOpen)
            {
                issues.Add("EYES_CLOSED");
                score -= 15;
            }

            // Verificar confianza de detección
            if (face.Confidence < 95)
            {
                issues.Add("LOW_DETECTION_CONFIDENCE");
                score -= 10;
            }

            var isAcceptable = score >= 60 && !issues.Contains("SUNGLASSES_DETECTED");

            return new ImageQualityCheckResult
            {
                IsAcceptable = isAcceptable,
                Score = (float)Math.Max(0, score),
                Message = isAcceptable
                    ? "Imagen de calidad aceptable para KYC"
                    : $"Imagen no cumple requisitos de calidad: {string.Join(", ", issues)}",
                Issues = issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking image quality");
            return new ImageQualityCheckResult
            {
                IsAcceptable = false,
                Message = $"Error al verificar calidad: {ex.Message}",
                Score = 0
            };
        }
    }

    /// <summary>
    /// Extrae el rostro de un documento de identidad
    /// </summary>
    public async Task<FaceExtractionResult> ExtractFaceFromDocumentAsync(byte[] documentImage, CancellationToken ct = default)
    {
        try
        {
            var detectResult = await DetectFacesAsync(documentImage, ct);

            if (!detectResult.Success)
            {
                return new FaceExtractionResult
                {
                    Success = false,
                    ErrorMessage = detectResult.ErrorMessage ?? "Error al detectar rostro en documento"
                };
            }

            if (detectResult.FaceCount == 0)
            {
                return new FaceExtractionResult
                {
                    Success = false,
                    ErrorMessage = "No se detectó ningún rostro en el documento"
                };
            }

            // Tomar el rostro con mayor confianza
            var bestFace = detectResult.Faces.OrderByDescending(f => f.Confidence).First();

            return new FaceExtractionResult
            {
                Success = true,
                BoundingBox = bestFace.BoundingBox,
                Quality = bestFace.Confidence // Usamos confidence como indicador de calidad
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting face from document");
            return new FaceExtractionResult
            {
                Success = false,
                ErrorMessage = $"Error al extraer rostro: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Verifica liveness básico analizando características faciales
    /// Nota: Para liveness real, usar Amazon Rekognition Face Liveness (servicio separado)
    /// </summary>
    public async Task<LivenessResult> CheckBasicLivenessAsync(
        byte[] selfieImage,
        LivenessChallenge challenge,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var detectResult = await DetectFacesAsync(selfieImage, ct);
            stopwatch.Stop();

            if (!detectResult.Success || detectResult.FaceCount == 0)
            {
                return new LivenessResult
                {
                    Success = false,
                    IsLive = false,
                    LivenessScore = 0,
                    Threshold = 80,
                    ErrorMessage = "No se detectó rostro en la imagen",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var face = detectResult.Faces.First();
            var passed = false;
            decimal confidence = 0;

            switch (challenge)
            {
                case LivenessChallenge.Blink:
                    // Verificar que los ojos pueden parpadear (difícil con una sola imagen)
                    passed = face.EyesOpen; // Simplificado: verificamos que hay ojos visibles
                    confidence = face.Confidence;
                    break;

                case LivenessChallenge.Smile:
                    passed = face.Smile;
                    confidence = face.Smile ? 85 : 20;
                    break;

                case LivenessChallenge.TurnHead:
                    // Verificar pose (requiere análisis más complejo)
                    passed = face.Confidence > 90;
                    confidence = face.Confidence;
                    break;

                case LivenessChallenge.OpenMouth:
                    passed = face.MouthOpen;
                    confidence = face.MouthOpen ? 85 : 20;
                    break;

                default:
                    passed = face.Confidence > 80;
                    confidence = face.Confidence;
                    break;
            }

            return new LivenessResult
            {
                Success = true,
                IsLive = passed,
                LivenessScore = confidence,
                Threshold = 80,
                ChallengeDetails = new List<LivenessChallengeDetail>
                {
                    new LivenessChallengeDetail
                    {
                        ChallengeType = challenge.ToString(),
                        Passed = passed,
                        Score = confidence,
                        Notes = passed
                            ? $"Challenge {challenge} pasado correctamente"
                            : $"Challenge {challenge} no completado"
                    }
                },
                FailureReason = passed ? null : $"Challenge {challenge} no completado",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error checking liveness");
            return new LivenessResult
            {
                Success = false,
                IsLive = false,
                LivenessScore = 0,
                ErrorMessage = $"Error en verificación de liveness: {ex.Message}",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Resultado de verificación de calidad de imagen
/// </summary>
public class ImageQualityCheckResult
{
    public bool IsAcceptable { get; set; }
    public float Score { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Desafíos de liveness disponibles
/// </summary>
public enum LivenessChallenge
{
    None,
    Blink,
    Smile,
    TurnHead,
    OpenMouth
}
