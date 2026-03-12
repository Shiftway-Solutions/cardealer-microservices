using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using MediaService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using DomainImageInfo = MediaService.Domain.Interfaces.Services.ImageInfo;

namespace MediaService.Infrastructure.Services.Processing;

public class ImageSharpProcessor : IImageProcessor
{
    private readonly ILogger<ImageSharpProcessor> _logger;

    public ImageSharpProcessor(ILogger<ImageSharpProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<ImageProcessingResult> ProcessImageAsync(Stream imageStream, string originalFileName, IEnumerable<ImageVariantConfig> variants)
    {
        var result = new ImageProcessingResult();

        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            // Corregir orientación basada en EXIF
            image.Mutate(x => x.AutoOrient());

            result.OriginalImageInfo = new DomainImageInfo
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                FileSize = imageStream.Length,
                HasAlpha = HasAlphaChannel(image),
                ColorSpace = GetColorSpace(image)
            };

            // Extraer metadatos EXIF si existen
            ExtractExifMetadata(image, result.OriginalImageInfo);

            foreach (var variantConfig in variants)
            {
                try
                {
                    imageStream.Position = 0;
                    using var variantImage = await Image.LoadAsync(imageStream);
                    variantImage.Mutate(x => x.AutoOrient());

                    var processedVariant = await ProcessVariantAsync(variantImage, variantConfig);
                    result.Variants.Add(processedVariant);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process variant {VariantName}", variantConfig.Name);
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Image processing failed");
        }

        return result;
    }

    public async Task<DomainImageInfo> GetImageInfoAsync(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var info = new DomainImageInfo
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                FileSize = imageStream.Length,
                HasAlpha = HasAlphaChannel(image),
                ColorSpace = GetColorSpace(image)
            };

            ExtractExifMetadata(image, info);

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image info");
            throw;
        }
    }

    public async Task<bool> ValidateImageAsync(Stream imageStream, string contentType)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);
            return image.Width > 0 && image.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stream> OptimizeImageAsync(Stream imageStream, string contentType, int quality)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var encoder = GetEncoder(contentType, quality);
            var outputStream = new MemoryStream();

            await image.SaveAsync(outputStream, encoder);
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image optimization failed");
            throw;
        }
    }

    public async Task<Stream> CreateThumbnailAsync(Stream imageStream, int width, int height, string resizeMode = "Max")
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var resizeOptions = new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = GetResizeMode(resizeMode)
            };

            image.Mutate(x => x.Resize(resizeOptions));

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 80 });
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail creation failed");
            throw;
        }
    }

    private async Task<ProcessedVariant> ProcessVariantAsync(Image image, ImageVariantConfig config)
    {
        var resizeOptions = new ResizeOptions
        {
            Size = new Size(config.MaxWidth, config.MaxHeight),
            Mode = GetResizeMode(config.ResizeMode)
        };

        image.Mutate(x => x.Resize(resizeOptions));

        var encoder = GetEncoder(config.Format, config.Quality);
        var outputStream = new MemoryStream();

        await image.SaveAsync(outputStream, encoder);
        outputStream.Position = 0;

        return new ProcessedVariant
        {
            Name = config.Name,
            ImageStream = outputStream,
            ContentType = GetContentType(config.Format),
            SizeBytes = outputStream.Length,
            Width = image.Width,
            Height = image.Height,
            StorageKey = $"{config.Name}_{image.Width}x{image.Height}"
        };
    }

    private static ResizeMode GetResizeMode(string mode)
    {
        return mode.ToLower() switch
        {
            "crop" => ResizeMode.Crop,
            "pad" => ResizeMode.Pad,
            "boxpad" => ResizeMode.BoxPad,
            "max" => ResizeMode.Max,
            "min" => ResizeMode.Min,
            "stretch" => ResizeMode.Stretch,
            _ => ResizeMode.Max
        };
    }

    private static IImageEncoder GetEncoder(string format, int quality)
    {
        return format.ToLower() switch
        {
            "png" => new PngEncoder(),
            "webp" => new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = quality },
            _ => new JpegEncoder { Quality = quality }
        };
    }

    private static string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "png" => "image/png",
            "webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static bool HasAlphaChannel(Image image)
    {
        return image.Metadata.DecodedImageFormat?.Name?.ToLower() == "png" ||
               image.Metadata.DecodedImageFormat?.Name?.ToLower() == "webp";
    }

    private static string GetColorSpace(Image image)
    {
        // Simplificación - en producción analizar el perfil ICC
        return "sRGB";
    }

    private void ExtractExifMetadata(Image image, DomainImageInfo info)
    {
        try
        {
            var exifProfile = image.Metadata.ExifProfile;
            if (exifProfile == null) return;

            // Extraer calidad estimada de JPEG
            if (image.Metadata.DecodedImageFormat?.Name?.ToLower() == "jpeg")
            {
                info.Quality = EstimateJpegQuality(image);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract EXIF metadata");
        }
    }

    private int EstimateJpegQuality(Image image)
    {
        // Estimación simplificada basada en dimensiones
        // En producción real: analizar tablas de cuantización JPEG
        var pixelCount = image.Width * image.Height;

        // Heurística simple: imágenes más grandes tienden a usar compresión más alta
        return pixelCount switch
        {
            > 4000000 => 85, // > 4MP
            > 2000000 => 90, // > 2MP
            > 1000000 => 92, // > 1MP
            _ => 95 // Pequeñas, probablemente alta calidad
        };
    }

    #region Advanced Image Processing Methods

    /// <summary>
    /// Aplica filtros avanzados a una imagen
    /// </summary>
    public async Task<Stream> ApplyFilterAsync(Stream imageStream, string filterType, Dictionary<string, object>? parameters = null)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            image.Mutate(x =>
            {
                switch (filterType.ToLower())
                {
                    case "blur":
                        var blurAmount = parameters?.ContainsKey("amount") == true
                            ? Convert.ToSingle(parameters["amount"])
                            : 5f;
                        x.GaussianBlur(blurAmount);
                        break;

                    case "sharpen":
                        var sharpenAmount = parameters?.ContainsKey("amount") == true
                            ? Convert.ToSingle(parameters["amount"])
                            : 1f;
                        x.GaussianSharpen(sharpenAmount);
                        break;

                    case "grayscale":
                        x.Grayscale();
                        break;

                    case "sepia":
                        x.Sepia();
                        break;

                    case "brightness":
                        var brightnessAmount = parameters?.ContainsKey("amount") == true
                            ? Convert.ToSingle(parameters["amount"])
                            : 1.5f;
                        x.Brightness(brightnessAmount);
                        break;

                    case "contrast":
                        var contrastAmount = parameters?.ContainsKey("amount") == true
                            ? Convert.ToSingle(parameters["amount"])
                            : 1.5f;
                        x.Contrast(contrastAmount);
                        break;

                    case "saturate":
                        var saturateAmount = parameters?.ContainsKey("amount") == true
                            ? Convert.ToSingle(parameters["amount"])
                            : 1.5f;
                        x.Saturate(saturateAmount);
                        break;

                    case "invert":
                        x.Invert();
                        break;

                    case "pixelate":
                        var pixelSize = parameters?.ContainsKey("size") == true
                            ? Convert.ToInt32(parameters["size"])
                            : 10;
                        x.Pixelate(pixelSize);
                        break;

                    default:
                        _logger.LogWarning("Unknown filter type: {FilterType}", filterType);
                        break;
                }
            });

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 90 });
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Filter application failed for {FilterType}", filterType);
            throw;
        }
    }

    /// <summary>
    /// Detecta el color dominante en una imagen
    /// </summary>
    public async Task<(int R, int G, int B)> GetDominantColorAsync(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync<Rgba32>(imageStream);

            // Reducir imagen para análisis más rápido
            var sampleSize = 100;
            image.Mutate(x => x.Resize(sampleSize, sampleSize));

            var colorCounts = new Dictionary<Rgba32, int>();
            var totalPixels = 0;

            // Analizar píxeles usando indexador correcto
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        var pixel = row[x];

                        // Ignorar píxeles muy oscuros o muy claros
                        if (pixel.R < 20 && pixel.G < 20 && pixel.B < 20) continue;
                        if (pixel.R > 235 && pixel.G > 235 && pixel.B > 235) continue;

                        // Cuantizar color (reducir precisión)
                        var quantized = new Rgba32(
                            (byte)((pixel.R / 32) * 32),
                            (byte)((pixel.G / 32) * 32),
                            (byte)((pixel.B / 32) * 32)
                    );

                        colorCounts.TryGetValue(quantized, out var count);
                        colorCounts[quantized] = count + 1;
                        totalPixels++;
                    }
                }
            });

            // Encontrar color más frecuente
            var dominantColor = colorCounts
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

            return (dominantColor.R, dominantColor.G, dominantColor.B);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect dominant color");
            return (128, 128, 128); // Gray default
        }
    }

    /// <summary>
    /// Reduce el ruido en una imagen
    /// </summary>
    public async Task<Stream> DenoiseAsync(Stream imageStream, float strength = 0.5f)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            // Aplicar filtro de reducción de ruido (mediana o bilateral simulado con blur suave)
            image.Mutate(x =>
            {
                x.GaussianBlur(strength * 2);
                x.GaussianSharpen(strength * 0.5f);
            });

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 92 });
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Denoising failed");
            throw;
        }
    }

    /// <summary>
    /// Genera un sprite sheet de múltiples variantes
    /// </summary>
    public async Task<Stream> GenerateSpriteSheetAsync(List<Stream> imageStreams, int columns, int padding = 10)
    {
        try
        {
            var images = new List<Image>();
            foreach (var stream in imageStreams)
            {
                stream.Position = 0;
                images.Add(await Image.LoadAsync(stream));
            }

            if (images.Count == 0)
            {
                throw new ArgumentException("No images provided for sprite sheet");
            }

            // Calcular dimensiones del sprite sheet
            var maxWidth = images.Max(img => img.Width);
            var maxHeight = images.Max(img => img.Height);
            var rows = (int)Math.Ceiling((double)images.Count / columns);

            var spriteWidth = (maxWidth * columns) + (padding * (columns + 1));
            var spriteHeight = (maxHeight * rows) + (padding * (rows + 1));

            // Crear sprite sheet
            using var spriteSheet = new Image<Rgba32>(spriteWidth, spriteHeight);
            spriteSheet.Mutate(x => x.BackgroundColor(Color.Transparent));

            // Colocar imágenes
            for (int i = 0; i < images.Count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                var x = padding + (col * (maxWidth + padding));
                var y = padding + (row * (maxHeight + padding));

                spriteSheet.Mutate(ctx => ctx.DrawImage(images[i], new Point(x, y), 1f));
            }

            // Limpiar imágenes temporales
            foreach (var img in images)
            {
                img.Dispose();
            }

            var outputStream = new MemoryStream();
            await spriteSheet.SaveAsync(outputStream, new PngEncoder());
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sprite sheet generation failed");
            throw;
        }
    }

    /// <summary>
    /// Compresión adaptativa basada en contenido
    /// </summary>
    public async Task<Stream> AdaptiveCompressAsync(Stream imageStream, long targetSizeBytes)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var currentQuality = 95;
            var minQuality = 50;
            Stream? bestResult = null;
            var bestSize = long.MaxValue;

            // Búsqueda binaria de calidad óptima
            while (currentQuality >= minQuality)
            {
                var testStream = new MemoryStream();
                await image.SaveAsync(testStream, new JpegEncoder { Quality = currentQuality });

                if (testStream.Length <= targetSizeBytes)
                {
                    bestResult?.Dispose();
                    bestResult = testStream;
                    bestSize = testStream.Length;
                    break;
                }

                if (testStream.Length < bestSize)
                {
                    bestResult?.Dispose();
                    bestResult = testStream;
                    bestSize = testStream.Length;
                }
                else
                {
                    testStream.Dispose();
                }

                currentQuality -= 5;
            }

            // Si no alcanzamos el target, intentar reducir dimensiones
            if (bestSize > targetSizeBytes && currentQuality < minQuality)
            {
                var scaleFactor = Math.Sqrt((double)targetSizeBytes / bestSize);
                var newWidth = (int)(image.Width * scaleFactor);
                var newHeight = (int)(image.Height * scaleFactor);

                image.Mutate(x => x.Resize(newWidth, newHeight));

                bestResult?.Dispose();
                bestResult = new MemoryStream();
                await image.SaveAsync(bestResult, new JpegEncoder { Quality = 85 });
            }

            if (bestResult == null)
            {
                throw new InvalidOperationException("Failed to compress image to target size");
            }

            bestResult.Position = 0;
            return bestResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adaptive compression failed");
            throw;
        }
    }

    /// <summary>
    /// Agrega marca de agua a una imagen
    /// </summary>
    public async Task<Stream> AddWatermarkAsync(
        Stream imageStream,
        string watermarkText,
        string position = "bottomright",
        float opacity = 0.5f,
        int fontSize = 24)
    {
        try
        {
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            // Simplificado - en producción usar SixLabors.Fonts
            // Por ahora, retornar la imagen sin marca de agua
            // Para implementación completa: dotnet add package SixLabors.Fonts

            _logger.LogWarning("Watermark feature requires SixLabors.Fonts package. Returning image without watermark.");

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 90 });
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Watermark addition failed");
            throw;
        }
    }

    #endregion
}