using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KYCService.Infrastructure.ExternalServices;

namespace KYCService.Infrastructure;

/// <summary>
/// Extensiones para configurar los servicios de infraestructura de KYC
/// 
/// NOTA: En República Dominicana NO existe una API pública de la JCE.
/// La verificación KYC se basa en:
/// 1. OCR - Extraer datos de la foto de la cédula
/// 2. Comparación de datos - Verificar que los datos extraídos coinciden con lo registrado
/// 3. Face comparison - Comparar la foto de la cédula con el selfie
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los servicios de infraestructura de KYC al contenedor de DI
    /// </summary>
    public static IServiceCollection AddKYCInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar Data Validation Service (reemplaza a JCE que no existe en RD)
        services.Configure<DataValidationConfig>(
            configuration.GetSection("DataValidation"));

        services.AddScoped<IDataValidationService, DataValidationService>();

        // Mantener JCE Service por compatibilidad (solo validación local)
        services.Configure<JCEServiceConfig>(
            configuration.GetSection(JCEServiceConfig.SectionName));

        services.AddHttpClient<IJCEService, JCEService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configurar OCR Service
        services.Configure<OCRServiceConfig>(
            configuration.GetSection(OCRServiceConfig.SectionName));

        services.AddSingleton<IOCRService, TesseractOCRService>();

        // Configurar Face Comparison Service
        services.Configure<FaceComparisonConfig>(
            configuration.GetSection(FaceComparisonConfig.SectionName));

        // Configurar Amazon Rekognition (RECOMENDADO: económico ~$0.001/imagen)
        services.Configure<AmazonRekognitionConfig>(
            configuration.GetSection("AmazonRekognition"));

        services.AddSingleton<AmazonRekognitionService>();

        services.AddScoped<IFaceComparisonService, FaceComparisonService>();

        return services;
    }

    /// <summary>
    /// Agrega configuración de desarrollo (lee de appsettings, permite simulación o Rekognition)
    /// </summary>
    public static IServiceCollection AddKYCInfrastructureDevelopment(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar Data Validation (siempre funciona localmente)
        services.Configure<DataValidationConfig>(options =>
        {
            options.NameMatchThreshold = 80; // Más permisivo en desarrollo
            options.MinimumMatchScore = 75;
            options.AllowFuzzyNameMatch = true;
        });

        // JCE solo hace validación local (no hay API externa en RD)
        services.Configure<JCEServiceConfig>(options =>
        {
            options.SimulationEnabled = true;
            options.ExternalValidationEnabled = false;
        });

        services.Configure<OCRServiceConfig>(options =>
        {
            options.UseSimulation = true;
        });

        // FaceComparison: Lee configuración de appsettings (permite activar Rekognition en Docker)
        // No se hardcodea simulación - se respeta lo que diga appsettings.Development.json
        // o las variables de entorno (FaceComparison__UseSimulation, etc.)

        return services.AddKYCInfrastructure(configuration);
    }

    /// <summary>
    /// Agrega configuración de producción (servicios reales)
    /// </summary>
    public static IServiceCollection AddKYCInfrastructureProduction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar Data Validation para producción
        services.Configure<DataValidationConfig>(options =>
        {
            options.NameMatchThreshold = 85;
            options.MinimumMatchScore = 80;
            options.AllowFuzzyNameMatch = true;
        });

        // JCE solo validación local (NO EXISTE API pública en RD)
        services.Configure<JCEServiceConfig>(options =>
        {
            options.SimulationEnabled = false;
            options.ExternalValidationEnabled = false; // No hay API de JCE
        });

        services.Configure<OCRServiceConfig>(options =>
        {
            options.UseSimulation = false;
        });

        // PRODUCCIÓN: Usar Amazon Rekognition (ECONÓMICO ~$0.001/imagen)
        services.Configure<FaceComparisonConfig>(options =>
        {
            options.UseSimulation = false;
            options.UseAmazonRekognition = true;  // ✅ RECOMENDADO: Económico
            options.UseAzureFaceApi = false;      // No usar Azure (más caro)
        });

        // Configurar Amazon Rekognition para producción
        services.Configure<AmazonRekognitionConfig>(options =>
        {
            options.Region = configuration["AWS:Region"] ?? "us-east-1";
            options.SimilarityThreshold = 80f;
        });

        return services.AddKYCInfrastructure(configuration);
    }
}
