using AdminService.Domain.Enums;

namespace AdminService.Domain.Entities;

/// <summary>
/// Representa un usuario administrador de la plataforma OKLA.
/// Esta entidad está vinculada a un User de AuthService pero contiene
/// información específica del rol administrativo.
/// </summary>
public class AdminUser
{
    /// <summary>
    /// Identificador único del registro de administrador.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID del usuario en AuthService/UserService.
    /// Vincula este registro con la cuenta de usuario principal.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Rol administrativo asignado.
    /// Define qué funcionalidades puede acceder este administrador.
    /// </summary>
    public AdminRole Role { get; set; }

    /// <summary>
    /// Nombre completo del administrador (cache local).
    /// Se sincroniza desde UserService.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email del administrador (cache local).
    /// Se sincroniza desde UserService.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Número de teléfono del administrador (cache local).
    /// Se sincroniza desde UserService.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Avatar URL del administrador.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Lista de permisos personalizados adicionales.
    /// Permite granularidad más allá del rol base.
    /// Formato: ["platform:config:edit", "platform:maintenance:toggle"]
    /// </summary>
    public List<string> CustomPermissions { get; set; } = new();

    /// <summary>
    /// Lista de permisos revocados del rol base.
    /// Permite restringir funcionalidades específicas.
    /// </summary>
    public List<string> RevokedPermissions { get; set; } = new();

    /// <summary>
    /// Indica si la cuenta de administrador está activa.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indica si la cuenta requiere MFA (Multi-Factor Authentication).
    /// </summary>
    public bool RequiresMfa { get; set; } = true;

    /// <summary>
    /// Indica si el MFA está configurado.
    /// </summary>
    public bool MfaEnabled { get; set; } = false;

    /// <summary>
    /// Método de MFA preferido.
    /// </summary>
    public MfaMethod? PreferredMfaMethod { get; set; }

    /// <summary>
    /// ID del administrador que creó esta cuenta.
    /// NULL si fue creado por el sistema.
    /// </summary>
    public Guid? CreatedByAdminId { get; set; }

    /// <summary>
    /// Fecha de creación del registro de administrador.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Fecha del último login exitoso.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// IP del último login.
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// User Agent del último login.
    /// </summary>
    public string? LastLoginUserAgent { get; set; }

    /// <summary>
    /// Número de intentos de login fallidos consecutivos.
    /// Se resetea después de un login exitoso.
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Fecha hasta la cual la cuenta está bloqueada.
    /// NULL si no está bloqueada.
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Notas internas sobre el administrador.
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Departamento o área del administrador.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Fecha de expiración de la cuenta de administrador.
    /// NULL para cuentas permanentes.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Indica si la cuenta está bloqueada actualmente.
    /// </summary>
    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    /// <summary>
    /// Indica si la cuenta ha expirado.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;

    /// <summary>
    /// Indica si el usuario puede acceder al sistema.
    /// </summary>
    public bool CanAccess => IsActive && !IsLocked && !IsExpired;

    // =========================================================================
    // Permission Check Methods
    // =========================================================================

    /// <summary>
    /// Verifica si el administrador tiene un permiso específico.
    /// Considera el rol base, permisos personalizados y permisos revocados.
    /// </summary>
    public bool HasPermission(string permission)
    {
        // Si está revocado, no tiene el permiso
        if (RevokedPermissions.Contains(permission))
            return false;

        // Si tiene permiso personalizado, lo tiene
        if (CustomPermissions.Contains(permission))
            return true;

        // Verificar según el rol
        return HasRolePermission(permission);
    }

    /// <summary>
    /// Verifica si el rol base tiene un permiso específico.
    /// </summary>
    private bool HasRolePermission(string permission)
    {
        // SuperAdmin tiene todos los permisos
        if (Role == AdminRole.SuperAdmin)
            return true;

        // Mapear permisos a roles
        return permission switch
        {
            // Platform Config
            "platform:config:view" => Role.CanManagePlatformConfig(),
            "platform:config:edit" => Role.CanManagePlatformConfig(),
            "platform:maintenance:toggle" => Role.CanManagePlatformConfig(),
            "platform:maintenance:schedule" => Role.CanManagePlatformConfig(),
            "platform:homepage:manage" => Role.CanManagePlatformConfig(),
            "platform:features:toggle" => Role.CanManagePlatformConfig(),

            // Moderation
            "platform:listings:view" => Role.CanModerateContent(),
            "platform:listings:approve" => Role.CanModerateContent(),
            "platform:listings:reject" => Role.CanModerateContent(),
            "platform:dealers:verify" => Role.CanVerifyDealers(),
            "platform:users:suspend" => Role.CanSuspendUsers(),
            "platform:users:ban" => Role.CanSuspendUsers(),
            "platform:reports:view" => Role.CanModerateContent(),
            "platform:reports:resolve" => Role.CanModerateContent(),

            // Support
            "platform:support:view" => Role.CanAccessSupport(),
            "platform:support:respond" => Role.CanAccessSupport(),
            "platform:users:impersonate" => Role.CanImpersonateUsers(),
            "platform:transactions:view" => Role.CanAccessSupport(),

            // Analytics
            "platform:analytics:view" => Role.CanViewAnalytics(),
            "platform:analytics:dashboard" => Role.CanViewAnalytics(),
            "platform:reports:export" => Role.CanExportReports(),

            // Admin Management
            "platform:admins:view" => Role.CanManageAdmins(),
            "platform:admins:create" => Role.CanManageAdmins(),
            "platform:admins:edit" => Role.CanManageAdmins(),
            "platform:admins:delete" => Role.CanManageAdmins(),

            // System
            "platform:logs:view" => Role.CanViewSystemLogs(),
            "platform:health:view" => Role.CanManagePlatformConfig(),

            _ => false
        };
    }

    /// <summary>
    /// Obtiene todos los permisos efectivos del administrador.
    /// </summary>
    public IEnumerable<string> GetEffectivePermissions()
    {
        var basePermissions = GetRolePermissions();

        return basePermissions
            .Union(CustomPermissions)
            .Except(RevokedPermissions)
            .Distinct();
    }

    /// <summary>
    /// Obtiene los permisos base del rol.
    /// </summary>
    private IEnumerable<string> GetRolePermissions()
    {
        return Role switch
        {
            AdminRole.SuperAdmin => AdminPermissions.All,
            AdminRole.PlatformAdmin => AdminPermissions.PlatformAdmin,
            AdminRole.ModerationAdmin => AdminPermissions.ModerationAdmin,
            AdminRole.SupportAdmin => AdminPermissions.SupportAdmin,
            AdminRole.AnalyticsAdmin => AdminPermissions.AnalyticsAdmin,
            _ => Enumerable.Empty<string>()
        };
    }

    // =========================================================================
    // Factory Methods
    // =========================================================================

    /// <summary>
    /// Crea un nuevo administrador con valores por defecto.
    /// </summary>
    public static AdminUser Create(
        Guid userId,
        AdminRole role,
        string fullName,
        string email,
        Guid? createdByAdminId = null)
    {
        return new AdminUser
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            FullName = fullName,
            Email = email,
            CreatedByAdminId = createdByAdminId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RequiresMfa = role != AdminRole.AnalyticsAdmin, // Analytics no requiere MFA obligatorio
        };
    }

    /// <summary>
    /// Registra un login exitoso.
    /// </summary>
    public void RecordSuccessfulLogin(string ipAddress, string userAgent)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        LastLoginUserAgent = userAgent;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// Registra un intento de login fallido.
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        // Bloquear después de 5 intentos fallidos
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
    }
}

/// <summary>
/// Método de autenticación multi-factor.
/// </summary>
public enum MfaMethod
{
    /// <summary>Autenticador TOTP (Google Authenticator, etc.)</summary>
    Authenticator = 0,

    /// <summary>SMS con código</summary>
    Sms = 1,

    /// <summary>Email con código</summary>
    Email = 2,

    /// <summary>Llave de seguridad FIDO2/WebAuthn</summary>
    SecurityKey = 3
}

/// <summary>
/// Constantes de permisos para administradores.
/// </summary>
public static class AdminPermissions
{
    // Platform Config
    public const string ConfigView = "platform:config:view";
    public const string ConfigEdit = "platform:config:edit";
    public const string MaintenanceToggle = "platform:maintenance:toggle";
    public const string MaintenanceSchedule = "platform:maintenance:schedule";
    public const string HomepageManage = "platform:homepage:manage";
    public const string FeaturesToggle = "platform:features:toggle";

    // Moderation
    public const string ListingsView = "platform:listings:view";
    public const string ListingsApprove = "platform:listings:approve";
    public const string ListingsReject = "platform:listings:reject";
    public const string DealersVerify = "platform:dealers:verify";
    public const string UsersSuspend = "platform:users:suspend";
    public const string UsersBan = "platform:users:ban";
    public const string ReportsView = "platform:reports:view";
    public const string ReportsResolve = "platform:reports:resolve";

    // Support
    public const string SupportView = "platform:support:view";
    public const string SupportRespond = "platform:support:respond";
    public const string UsersImpersonate = "platform:users:impersonate";
    public const string TransactionsView = "platform:transactions:view";

    // Analytics
    public const string AnalyticsView = "platform:analytics:view";
    public const string AnalyticsDashboard = "platform:analytics:dashboard";
    public const string ReportsExport = "platform:reports:export";

    // Admin Management
    public const string AdminsView = "platform:admins:view";
    public const string AdminsCreate = "platform:admins:create";
    public const string AdminsEdit = "platform:admins:edit";
    public const string AdminsDelete = "platform:admins:delete";

    // System
    public const string LogsView = "platform:logs:view";
    public const string HealthView = "platform:health:view";

    /// <summary>Todos los permisos (para SuperAdmin)</summary>
    public static readonly string[] All = new[]
    {
        ConfigView, ConfigEdit, MaintenanceToggle, MaintenanceSchedule,
        HomepageManage, FeaturesToggle,
        ListingsView, ListingsApprove, ListingsReject, DealersVerify,
        UsersSuspend, UsersBan, ReportsView, ReportsResolve,
        SupportView, SupportRespond, UsersImpersonate, TransactionsView,
        AnalyticsView, AnalyticsDashboard, ReportsExport,
        AdminsView, AdminsCreate, AdminsEdit, AdminsDelete,
        LogsView, HealthView
    };

    /// <summary>Permisos para PlatformAdmin</summary>
    public static readonly string[] PlatformAdmin = new[]
    {
        ConfigView, ConfigEdit, MaintenanceToggle, MaintenanceSchedule,
        HomepageManage, FeaturesToggle,
        AnalyticsView, AnalyticsDashboard, ReportsExport,
        LogsView, HealthView
    };

    /// <summary>Permisos para ModerationAdmin</summary>
    public static readonly string[] ModerationAdmin = new[]
    {
        ListingsView, ListingsApprove, ListingsReject,
        DealersVerify,
        UsersSuspend, UsersBan,
        ReportsView, ReportsResolve
    };

    /// <summary>Permisos para SupportAdmin</summary>
    public static readonly string[] SupportAdmin = new[]
    {
        SupportView, SupportRespond,
        UsersImpersonate,
        TransactionsView
    };

    /// <summary>Permisos para AnalyticsAdmin</summary>
    public static readonly string[] AnalyticsAdmin = new[]
    {
        AnalyticsView, AnalyticsDashboard, ReportsExport
    };
}
