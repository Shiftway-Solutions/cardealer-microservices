namespace AdminService.Domain.Enums;

/// <summary>
/// Roles específicos para administradores de la plataforma OKLA.
/// Define los niveles de acceso y responsabilidades de cada tipo de administrador.
/// </summary>
public enum AdminRole
{
    /// <summary>
    /// Acceso total a todas las funcionalidades de la plataforma.
    /// - Gestión de todos los admins
    /// - Configuración del sistema
    /// - Acceso a logs y auditoría
    /// - Todas las capacidades de otros roles
    /// </summary>
    SuperAdmin = 0,

    /// <summary>
    /// Administrador de plataforma con acceso a configuración.
    /// - Configuración del sistema
    /// - Modo mantenimiento
    /// - Homepage sections
    /// - Feature flags
    /// - Monitoreo de salud del sistema
    /// - NO puede gestionar otros admins
    /// </summary>
    PlatformAdmin = 1,

    /// <summary>
    /// Moderador de contenido.
    /// - Aprobar/rechazar listados de vehículos
    /// - Verificar dealers (documentos)
    /// - Suspender/banear usuarios
    /// - Gestionar reportes de usuarios
    /// - NO tiene acceso a configuración del sistema
    /// </summary>
    ModerationAdmin = 2,

    /// <summary>
    /// Soporte al cliente.
    /// - Ver información de usuarios (sin modificar)
    /// - Acceso a tickets de soporte
    /// - Ver detalles de transacciones
    /// - Impersonar usuarios (para debugging)
    /// - NO puede aprobar/rechazar contenido
    /// </summary>
    SupportAdmin = 3,

    /// <summary>
    /// Acceso solo lectura a analytics y reportes.
    /// - Ver dashboard de analytics
    /// - Exportar reportes
    /// - Ver métricas de plataforma
    /// - NO puede modificar nada
    /// </summary>
    AnalyticsAdmin = 4
}

/// <summary>
/// Extensiones para validar permisos según el rol de admin.
/// </summary>
public static class AdminRoleExtensions
{
    /// <summary>
    /// Verifica si el rol tiene acceso completo al sistema.
    /// </summary>
    public static bool HasFullAccess(this AdminRole role) => role == AdminRole.SuperAdmin;

    /// <summary>
    /// Verifica si el rol puede gestionar configuración de la plataforma.
    /// </summary>
    public static bool CanManagePlatformConfig(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.PlatformAdmin;

    /// <summary>
    /// Verifica si el rol puede moderar contenido (aprobar/rechazar).
    /// </summary>
    public static bool CanModerateContent(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.ModerationAdmin;

    /// <summary>
    /// Verifica si el rol puede verificar dealers.
    /// </summary>
    public static bool CanVerifyDealers(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.ModerationAdmin;

    /// <summary>
    /// Verifica si el rol puede suspender/banear usuarios.
    /// </summary>
    public static bool CanSuspendUsers(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.ModerationAdmin;

    /// <summary>
    /// Verifica si el rol tiene acceso a soporte.
    /// </summary>
    public static bool CanAccessSupport(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.SupportAdmin;

    /// <summary>
    /// Verifica si el rol puede impersonar usuarios.
    /// </summary>
    public static bool CanImpersonateUsers(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.SupportAdmin;

    /// <summary>
    /// Verifica si el rol puede ver analytics.
    /// </summary>
    public static bool CanViewAnalytics(this AdminRole role) =>
        role == AdminRole.SuperAdmin ||
        role == AdminRole.PlatformAdmin ||
        role == AdminRole.AnalyticsAdmin;

    /// <summary>
    /// Verifica si el rol puede exportar reportes.
    /// </summary>
    public static bool CanExportReports(this AdminRole role) =>
        role == AdminRole.SuperAdmin ||
        role == AdminRole.PlatformAdmin ||
        role == AdminRole.AnalyticsAdmin;

    /// <summary>
    /// Verifica si el rol puede gestionar otros administradores.
    /// </summary>
    public static bool CanManageAdmins(this AdminRole role) =>
        role == AdminRole.SuperAdmin;

    /// <summary>
    /// Verifica si el rol puede ver logs del sistema.
    /// </summary>
    public static bool CanViewSystemLogs(this AdminRole role) =>
        role == AdminRole.SuperAdmin || role == AdminRole.PlatformAdmin;

    /// <summary>
    /// Obtiene el nombre descriptivo del rol en español.
    /// </summary>
    public static string GetDisplayName(this AdminRole role) => role switch
    {
        AdminRole.SuperAdmin => "Super Administrador",
        AdminRole.PlatformAdmin => "Administrador de Plataforma",
        AdminRole.ModerationAdmin => "Administrador de Moderación",
        AdminRole.SupportAdmin => "Administrador de Soporte",
        AdminRole.AnalyticsAdmin => "Administrador de Analytics",
        _ => "Desconocido"
    };

    /// <summary>
    /// Obtiene la descripción del rol en español.
    /// </summary>
    public static string GetDescription(this AdminRole role) => role switch
    {
        AdminRole.SuperAdmin => "Acceso total a todas las funcionalidades de la plataforma",
        AdminRole.PlatformAdmin => "Configuración del sistema, mantenimiento y monitoreo",
        AdminRole.ModerationAdmin => "Aprobación de contenido y verificación de dealers",
        AdminRole.SupportAdmin => "Soporte al cliente y gestión de tickets",
        AdminRole.AnalyticsAdmin => "Acceso solo lectura a analytics y reportes",
        _ => "Sin descripción"
    };
}
