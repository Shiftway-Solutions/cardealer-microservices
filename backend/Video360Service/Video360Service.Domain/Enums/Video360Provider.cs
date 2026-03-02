namespace Video360Service.Domain.Enums;

/// <summary>
/// Proveedores disponibles para extracción de frames de videos 360
/// </summary>
public enum Video360Provider
{
    /// <summary>
    /// FFmpeg-API.com (Starter) - $11/mes, $0.011/vehículo
    /// Calidad: Excelente
    /// </summary>
    FfmpegApi = 0,
    
    /// <summary>
    /// ApyHub Video API - $9/mes, $0.009/vehículo
    /// Calidad: Muy buena
    /// </summary>
    ApyHub = 1,
    
    /// <summary>
    /// Cloudinary (pago) - $12/mes, $0.012/vehículo
    /// Calidad: Buena
    /// </summary>
    Cloudinary = 2,
    
    /// <summary>
    /// Imgix - $18/mes, $0.018/vehículo
    /// Calidad: Excelente
    /// </summary>
    Imgix = 3,
    
    /// <summary>
    /// Shotstack - $50/mes, $0.05/vehículo
    /// Calidad: Profesional
    /// </summary>
    Shotstack = 4,
    
    /// <summary>
    /// Local FFmpeg - Procesamiento local sin costo ni API key
    /// Calidad: Alta (depends on hardware)
    /// </summary>
    Local = 5
}
