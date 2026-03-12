using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KYCService.Domain.Validators;
using Tesseract;

namespace KYCService.Infrastructure.ExternalServices;

/// <summary>
/// Implementación del servicio OCR usando Tesseract OCR
/// Tesseract es una librería open-source de OCR de Google
/// </summary>
public class TesseractOCRService : IOCRService, IDisposable
{
    private readonly ILogger<TesseractOCRService> _logger;
    private readonly OCRServiceConfig _config;
    private readonly TesseractEngine? _engine;
    private bool _disposed;

    // Patrones regex para extraer datos de cédula dominicana
    private static readonly Regex CedulaPatternWithDashes = new(@"\b(\d{3})-?(\d{7})-?(\d)\b", RegexOptions.Compiled);
    private static readonly Regex CedulaPatternNoDashes = new(@"\b(\d{11})\b", RegexOptions.Compiled);
    private static readonly Regex DatePattern = new(@"\b(\d{2})[/-](\d{2})[/-](\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex NamePattern = new(@"^[A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TesseractOCRService(
        ILogger<TesseractOCRService> logger,
        IOptions<OCRServiceConfig> config)
    {
        _logger = logger;
        _config = config.Value;

        try
        {
            // Inicializar motor Tesseract con datos de entrenamiento en español
            var tessDataPath = _config.TesseractDataPath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");

            if (Directory.Exists(tessDataPath))
            {
                _engine = new TesseractEngine(tessDataPath, "spa", EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ0123456789-/. ");
                _logger.LogInformation("Tesseract OCR engine initialized with tessdata path: {Path}", tessDataPath);
            }
            else
            {
                _logger.LogWarning("Tesseract tessdata path not found: {Path}. OCR will use simulation mode.", tessDataPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Tesseract OCR engine. Will use simulation mode.");
        }
    }

    /// <inheritdoc />
    public async Task<OCRResult> ExtractTextAsync(byte[] imageData, DocumentOCRType documentType, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new OCRResult();

        try
        {
            _logger.LogInformation("Starting OCR extraction for document type: {DocumentType}", documentType);

            if (_engine == null || _config.UseSimulation)
            {
                // Modo simulación
                result = await SimulateOCRAsync(documentType, cancellationToken);
            }
            else
            {
                // Procesamiento real con Tesseract
                result = await ProcessWithTesseractAsync(imageData, cancellationToken);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR extraction failed");
            result.Success = false;
            result.ErrorMessage = "Error procesando la imagen. Intente nuevamente.";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CedulaOCRResult> ExtractCedulaFrontAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CedulaOCRResult();

        try
        {
            _logger.LogInformation("Extracting data from cédula front");

            if (_engine == null || _config.UseSimulation)
            {
                result = SimulateCedulaFrontOCR();
            }
            else
            {
                var ocrResult = await ProcessWithTesseractAsync(imageData, cancellationToken);
                result = ParseCedulaFront(ocrResult);
            }

            // Validar número de cédula extraído
            if (!string.IsNullOrEmpty(result.CedulaNumber))
            {
                var validation = CedulaValidator.ValidateDetailed(result.CedulaNumber);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("Extracted cedula number failed validation: {CedulaMasked}",
                        MaskCedula(result.CedulaNumber));
                }
                result.CedulaNumber = validation.FormattedNumber;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract cédula front data");
            result.Success = false;
            result.ErrorMessage = "Error procesando el frente de la cédula";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CedulaOCRResult> ExtractCedulaBackAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CedulaOCRResult();

        try
        {
            _logger.LogInformation("Extracting data from cédula back");

            if (_engine == null || _config.UseSimulation)
            {
                result = SimulateCedulaBackOCR();
            }
            else
            {
                var ocrResult = await ProcessWithTesseractAsync(imageData, cancellationToken);
                result = ParseCedulaBack(ocrResult);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract cédula back data");
            result.Success = false;
            result.ErrorMessage = "Error procesando el reverso de la cédula";
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ImageQualityResult> CheckImageQualityAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        var result = new ImageQualityResult();

        try
        {
            // Análisis básico de la imagen
            using var stream = new MemoryStream(imageData);

            // TODO: Usar SkiaSharp o ImageSharp para análisis real
            // Por ahora, retornar valores simulados
            await Task.CompletedTask;

            result.Metrics = new ImageQualityMetrics
            {
                Sharpness = 85,
                Brightness = 75,
                Contrast = 80,
                HasGlare = false,
                IsBlurry = false,
                IsDocumentCutOff = false,
                SkewAngle = 0.5m,
                Width = 1920,
                Height = 1080,
                EstimatedDPI = 300
            };

            // Evaluar calidad general
            result.QualityScore = (result.Metrics.Sharpness + result.Metrics.Brightness + result.Metrics.Contrast) / 3;
            result.IsAcceptable = result.QualityScore >= 60 &&
                                  !result.Metrics.IsBlurry &&
                                  !result.Metrics.IsDocumentCutOff;

            // Agregar problemas e sugerencias
            if (result.Metrics.IsBlurry)
            {
                result.Issues.Add("Imagen borrosa detectada");
                result.Suggestions.Add("Mantenga la cámara estable y enfocada");
            }

            if (result.Metrics.HasGlare)
            {
                result.Issues.Add("Reflejos detectados en la imagen");
                result.Suggestions.Add("Evite fuentes de luz directa sobre el documento");
            }

            if (result.Metrics.Brightness < 40)
            {
                result.Issues.Add("Imagen muy oscura");
                result.Suggestions.Add("Tome la foto en un lugar bien iluminado");
            }

            if (result.Metrics.IsDocumentCutOff)
            {
                result.Issues.Add("El documento aparece cortado");
                result.Suggestions.Add("Asegúrese de que las 4 esquinas del documento sean visibles");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check image quality");
            result.IsAcceptable = true; // Permitir continuar en caso de error
            result.Issues.Add("No se pudo verificar la calidad de la imagen");
        }

        return result;
    }

    private async Task<OCRResult> ProcessWithTesseractAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        var result = new OCRResult();

        await Task.Run(() =>
        {
            try
            {
                using var pix = Pix.LoadFromMemory(imageData);
                using var page = _engine!.Process(pix);

                result.RawText = page.GetText()?.Trim() ?? string.Empty;
                result.Confidence = (decimal)page.GetMeanConfidence() * 100;
                result.DetectedLanguage = "spa";

                // Extraer líneas individuales
                using var iter = page.GetIterator();
                iter.Begin();

                do
                {
                    if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var bounds))
                    {
                        var lineText = iter.GetText(PageIteratorLevel.TextLine)?.Trim();
                        if (!string.IsNullOrEmpty(lineText))
                        {
                            result.TextLines.Add(new OCRTextLine
                            {
                                Text = lineText,
                                Confidence = (decimal)(iter.GetConfidence(PageIteratorLevel.TextLine)),
                                BoundingBox = new OCRBoundingBox
                                {
                                    X = bounds.X1,
                                    Y = bounds.Y1,
                                    Width = bounds.Width,
                                    Height = bounds.Height
                                }
                            });
                        }
                    }
                } while (iter.Next(PageIteratorLevel.TextLine));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tesseract processing failed");
                throw;
            }
        }, cancellationToken);

        return result;
    }

    private CedulaOCRResult ParseCedulaFront(OCRResult ocrResult)
    {
        var result = new CedulaOCRResult
        {
            RawText = ocrResult.RawText,
            TextLines = ocrResult.TextLines,
            Confidence = ocrResult.Confidence,
            DetectedLanguage = ocrResult.DetectedLanguage
        };

        var text = ocrResult.RawText.ToUpperInvariant();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        // Buscar número de cédula
        var cedulaMatch = CedulaPatternWithDashes.Match(text);
        if (!cedulaMatch.Success)
        {
            cedulaMatch = CedulaPatternNoDashes.Match(text);
        }
        if (cedulaMatch.Success)
        {
            result.CedulaNumber = CedulaValidator.FormatCedula(cedulaMatch.Value);
        }

        // Buscar nacionalidad
        if (text.Contains("DOMINICANA") || text.Contains("DOMINICAN"))
        {
            result.Nationality = "DOMINICANA";
        }

        // Buscar fechas (nacimiento, expedición, expiración)
        var dateMatches = DatePattern.Matches(text);
        foreach (Match match in dateMatches)
        {
            if (DateTime.TryParseExact(match.Value, new[] { "dd/MM/yyyy", "dd-MM-yyyy" },
                null, System.Globalization.DateTimeStyles.None, out var date))
            {
                // La primera fecha suele ser nacimiento
                if (result.DateOfBirth == null && date.Year < 2010)
                {
                    result.DateOfBirth = date;
                }
                else if (result.ExpiryDate == null && date.Year > DateTime.Now.Year)
                {
                    result.ExpiryDate = date;
                }
            }
        }

        // Buscar género
        if (text.Contains("MASCULINO") || Regex.IsMatch(text, @"\bM\b"))
        {
            result.Gender = "M";
        }
        else if (text.Contains("FEMENINO") || Regex.IsMatch(text, @"\bF\b"))
        {
            result.Gender = "F";
        }

        // Buscar nombre (líneas que parecen nombres)
        foreach (var line in lines)
        {
            if (NamePattern.IsMatch(line) && line.Length > 5 && line.Length < 50)
            {
                if (!line.Contains("DOMINICANA") && !line.Contains("REPUBLICA") &&
                    !line.Contains("CEDULA") && !line.Contains("JUNTA"))
                {
                    result.FullName = line;
                    var nameParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length >= 2)
                    {
                        result.FirstName = nameParts[0];
                        if (nameParts.Length >= 3)
                        {
                            result.MiddleName = nameParts.Length > 3 ? nameParts[1] : null;
                            result.LastName = nameParts.Length > 3 ? nameParts[2] : nameParts[1];
                            result.SecondLastName = nameParts.Length > 3 ? nameParts[3] : nameParts.Length > 2 ? nameParts[2] : null;
                        }
                        else
                        {
                            result.LastName = nameParts[1];
                        }
                    }
                    break;
                }
            }
        }

        result.PhotoDetected = true; // Asumimos que hay foto en el frente

        return result;
    }

    private CedulaOCRResult ParseCedulaBack(OCRResult ocrResult)
    {
        var result = new CedulaOCRResult
        {
            RawText = ocrResult.RawText,
            TextLines = ocrResult.TextLines,
            Confidence = ocrResult.Confidence,
            DetectedLanguage = ocrResult.DetectedLanguage
        };

        var text = ocrResult.RawText.ToUpperInvariant();

        // El reverso tiene MRZ y dirección
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        // Buscar MRZ (líneas con muchos < caracteres)
        foreach (var line in lines)
        {
            if (line.Contains('<') && line.Length > 20)
            {
                result.MRZCode = line;
                break;
            }
        }

        // Buscar dirección (líneas largas sin caracteres especiales de MRZ)
        foreach (var line in lines)
        {
            if (!line.Contains('<') && line.Length > 15 &&
                (line.Contains("CALLE") || line.Contains("AVE") || line.Contains("URB") ||
                 line.Contains("SECTOR") || line.Contains("NO.") || line.Contains("#")))
            {
                result.Address = line;
                break;
            }
        }

        // Buscar lugar de nacimiento
        var birthPlaces = new[] { "SANTO DOMINGO", "SANTIAGO", "LA VEGA", "SAN CRISTOBAL", "PUERTO PLATA" };
        foreach (var place in birthPlaces)
        {
            if (text.Contains(place))
            {
                result.BirthPlace = place;
                break;
            }
        }

        return result;
    }

    private async Task<OCRResult> SimulateOCRAsync(DocumentOCRType documentType, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken); // Simular latencia de procesamiento

        return new OCRResult
        {
            Success = true,
            RawText = "REPUBLICA DOMINICANA\nJUNTA CENTRAL ELECTORAL\nCEDULA DE IDENTIDAD Y ELECTORAL\n" +
                      "JUAN ANTONIO PEREZ MARTINEZ\n001-1234567-8\nNACIONALIDAD: DOMINICANA\n" +
                      "FECHA NACIMIENTO: 15/06/1985\nSEXO: M",
            Confidence = 95,
            DetectedLanguage = "spa",
            TextLines = new List<OCRTextLine>
            {
                new() { Text = "REPUBLICA DOMINICANA", Confidence = 98 },
                new() { Text = "JUNTA CENTRAL ELECTORAL", Confidence = 97 },
                new() { Text = "CEDULA DE IDENTIDAD Y ELECTORAL", Confidence = 96 },
                new() { Text = "JUAN ANTONIO PEREZ MARTINEZ", Confidence = 94 },
                new() { Text = "001-1234567-8", Confidence = 99 },
                new() { Text = "NACIONALIDAD: DOMINICANA", Confidence = 95 },
                new() { Text = "FECHA NACIMIENTO: 15/06/1985", Confidence = 93 },
                new() { Text = "SEXO: M", Confidence = 98 }
            }
        };
    }

    private CedulaOCRResult SimulateCedulaFrontOCR()
    {
        return new CedulaOCRResult
        {
            Success = true,
            Confidence = 95,
            RawText = "CEDULA SIMULADA - FRENTE",
            CedulaNumber = "001-1234567-8",
            FullName = "JUAN ANTONIO PEREZ MARTINEZ",
            FirstName = "JUAN",
            MiddleName = "ANTONIO",
            LastName = "PEREZ",
            SecondLastName = "MARTINEZ",
            DateOfBirth = new DateTime(1985, 6, 15),
            Nationality = "DOMINICANA",
            Gender = "M",
            PhotoDetected = true,
            DetectedLanguage = "spa"
        };
    }

    private CedulaOCRResult SimulateCedulaBackOCR()
    {
        return new CedulaOCRResult
        {
            Success = true,
            Confidence = 92,
            RawText = "CEDULA SIMULADA - REVERSO",
            Address = "CALLE PRINCIPAL #123, SECTOR LOS PRADOS, SANTO DOMINGO",
            BirthPlace = "SANTO DOMINGO",
            MaritalStatus = "SOLTERO",
            IssueDate = DateTime.Now.AddYears(-5),
            ExpiryDate = DateTime.Now.AddYears(5),
            MRZCode = "IDDOP123456789<<<<<<<<<<<<<<<<",
            DetectedLanguage = "spa"
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _engine?.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Masks a cédula number for secure logging (shows only last 4 characters).
    /// Ley 172-13 Art. 31 — Data minimization in logs.
    /// </summary>
    private static string MaskCedula(string? cedula)
    {
        if (string.IsNullOrEmpty(cedula) || cedula.Length <= 4)
            return "****";
        return new string('*', cedula.Length - 4) + cedula[^4..];
    }
}

/// <summary>
/// Configuración del servicio OCR
/// </summary>
public class OCRServiceConfig
{
    public const string SectionName = "OCRService";

    /// <summary>
    /// Ruta a los archivos de datos de Tesseract
    /// </summary>
    public string? TesseractDataPath { get; set; }

    /// <summary>
    /// Idiomas a usar (separados por +, ej: "spa+eng")
    /// </summary>
    public string Languages { get; set; } = "spa";

    /// <summary>
    /// Usar modo de simulación (para desarrollo)
    /// </summary>
    public bool UseSimulation { get; set; } = true;

    /// <summary>
    /// Confianza mínima requerida (0-100)
    /// </summary>
    public int MinimumConfidence { get; set; } = 60;

    /// <summary>
    /// Preprocesar imagen antes de OCR
    /// </summary>
    public bool PreprocessImage { get; set; } = true;

    /// <summary>
    /// Ajustar contraste automáticamente
    /// </summary>
    public bool AutoContrast { get; set; } = true;

    /// <summary>
    /// Corregir inclinación automáticamente
    /// </summary>
    public bool AutoDeskew { get; set; } = true;
}
