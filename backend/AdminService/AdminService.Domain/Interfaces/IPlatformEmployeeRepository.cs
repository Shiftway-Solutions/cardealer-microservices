using AdminService.Domain.Entities;

namespace AdminService.Domain.Interfaces;

/// <summary>
/// Repositorio para gestión de empleados de plataforma
/// </summary>
public interface IPlatformEmployeeRepository
{
    // ========================================
    // EMPLOYEES
    // ========================================

    /// <summary>
    /// Obtener empleado por ID
    /// </summary>
    Task<PlatformEmployee?> GetByIdAsync(Guid id);

    /// <summary>
    /// Obtener empleado por email
    /// </summary>
    Task<PlatformEmployee?> GetByEmailAsync(string email);

    /// <summary>
    /// Obtener empleado por user ID
    /// </summary>
    Task<PlatformEmployee?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Obtener todos los empleados con paginación y filtros
    /// </summary>
    Task<(IEnumerable<PlatformEmployee> Employees, int TotalCount)> GetAllAsync(
        string? status = null,
        string? role = null,
        string? department = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Crear nuevo empleado
    /// </summary>
    Task<PlatformEmployee> AddAsync(PlatformEmployee employee);

    /// <summary>
    /// Actualizar empleado
    /// </summary>
    Task UpdateAsync(PlatformEmployee employee);

    /// <summary>
    /// Soft delete de empleado
    /// </summary>
    Task SoftDeleteAsync(Guid id);

    // ========================================
    // INVITATIONS
    // ========================================

    /// <summary>
    /// Obtener invitación por ID
    /// </summary>
    Task<PlatformEmployeeInvitation?> GetInvitationByIdAsync(Guid id);

    /// <summary>
    /// Obtener invitación por token
    /// </summary>
    Task<PlatformEmployeeInvitation?> GetInvitationByTokenAsync(string token);

    /// <summary>
    /// Obtener invitación pendiente por email
    /// </summary>
    Task<PlatformEmployeeInvitation?> GetPendingInvitationByEmailAsync(string email);

    /// <summary>
    /// Obtener todas las invitaciones con filtro opcional de estado
    /// </summary>
    Task<IEnumerable<PlatformEmployeeInvitation>> GetInvitationsAsync(InvitationStatus? status = null);

    /// <summary>
    /// Crear nueva invitación
    /// </summary>
    Task<PlatformEmployeeInvitation> AddInvitationAsync(PlatformEmployeeInvitation invitation);

    /// <summary>
    /// Actualizar invitación
    /// </summary>
    Task UpdateInvitationAsync(PlatformEmployeeInvitation invitation);

    // ========================================
    // ACTIVITY
    // ========================================

    /// <summary>
    /// Obtener actividad de un empleado
    /// </summary>
    Task<(IEnumerable<EmployeeActivity> Activities, int TotalCount)> GetActivityAsync(
        Guid employeeId,
        DateTime from,
        DateTime to,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Registrar actividad de un empleado
    /// </summary>
    Task LogActivityAsync(EmployeeActivity activity);
}
