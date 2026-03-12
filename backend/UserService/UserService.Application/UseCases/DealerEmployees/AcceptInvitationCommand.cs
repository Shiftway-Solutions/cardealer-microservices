using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerEmployees;

/// <summary>
/// DTO de respuesta cuando se acepta una invitación
/// </summary>
public class AcceptInvitationResponse
{
    public Guid UserId { get; set; }
    public Guid DealerId { get; set; }
    public string DealerName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
}

/// <summary>
/// Command para aceptar una invitación de empleado
/// </summary>
public record AcceptInvitationCommand(
    string Token,
    string? Password,      // Solo si es nuevo usuario
    string? FirstName,     // Solo si es nuevo usuario
    string? LastName,      // Solo si es nuevo usuario
    string? Phone          // Opcional
) : IRequest<AcceptInvitationResponse>;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, AcceptInvitationResponse>
{
    private readonly IDealerEmployeeRepository _employeeRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        IDealerEmployeeRepository employeeRepository,
        IDealerRepository dealerRepository,
        IUserRepository userRepository,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _dealerRepository = dealerRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AcceptInvitationResponse> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing invitation acceptance for token ...{TokenSuffix}", request.Token.Length > 8 ? request.Token[^8..] : "***");

        // 1. Find the invitation by token
        var invitation = await _employeeRepository.GetInvitationByTokenAsync(request.Token);
        if (invitation == null)
        {
            throw new NotFoundException("Invitación no encontrada o token inválido");
        }

        // 2. Validate invitation status
        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new BadRequestException($"Esta invitación ya fue {invitation.Status.ToString().ToLower()}");
        }

        // 3. Check expiration
        if (invitation.ExpirationDate < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _employeeRepository.UpdateInvitationAsync(invitation);
            throw new BadRequestException("Esta invitación ha expirado. Solicita una nueva invitación.");
        }

        // 4. Get dealer info
        var dealer = await _dealerRepository.GetByIdAsync(invitation.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException("El dealer asociado a esta invitación ya no existe");
        }

        // 5. Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(invitation.Email);
        bool isNewUser = existingUser == null;
        User user;

        if (isNewUser)
        {
            // Validate required fields for new user
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new BadRequestException("Se requiere una contraseña para nuevos usuarios");
            }
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                throw new BadRequestException("Se requiere nombre y apellido para nuevos usuarios");
            }

            // Create new user as DealerEmployee
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = invitation.Email,
                FirstName = request.FirstName!,
                LastName = request.LastName!,
                PhoneNumber = request.Phone ?? string.Empty,
                AccountType = AccountType.DealerEmployee,
                DealerId = invitation.DealerId,
                DealerRole = invitation.DealerRole,
                DealerPermissions = invitation.Permissions,
                EmployerUserId = invitation.InvitedBy,
                IsActive = true,
                IsEmailVerified = true, // Auto-verified since they accepted invitation
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // TODO: Hash password and create in AuthService
            // For now, we create the user record
            await _userRepository.AddAsync(user);
            _logger.LogInformation("Created new user {UserId} for invitation {InvitationId}", user.Id, invitation.Id);
        }
        else
        {
            user = existingUser!;

            // Check if already an employee of this dealer
            var existingEmployee = await _employeeRepository.GetByUserIdAndDealerIdAsync(user.Id, invitation.DealerId);
            if (existingEmployee != null)
            {
                throw new BadRequestException("Ya eres empleado de este dealer");
            }

            // Update existing user to be a DealerEmployee
            user.AccountType = AccountType.DealerEmployee;
            user.DealerId = invitation.DealerId;
            user.DealerRole = invitation.DealerRole;
            user.DealerPermissions = invitation.Permissions;
            user.EmployerUserId = invitation.InvitedBy;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Updated existing user {UserId} to dealer employee", user.Id);
        }

        // 6. Create DealerEmployee record
        var dealerEmployee = new DealerEmployee
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DealerId = invitation.DealerId,
            DealerRole = invitation.DealerRole,
            Permissions = invitation.Permissions,
            InvitedBy = invitation.InvitedBy,
            Status = EmployeeStatus.Active,
            InvitationDate = invitation.InvitationDate,
            ActivationDate = DateTime.UtcNow
        };

        await _employeeRepository.AddAsync(dealerEmployee);

        // 7. Update invitation status
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedDate = DateTime.UtcNow;
        await _employeeRepository.UpdateInvitationAsync(invitation);

        _logger.LogInformation("Invitation {InvitationId} accepted by user {UserId}", invitation.Id, user.Id);

        // TODO: Send welcome email via NotificationService

        return new AcceptInvitationResponse
        {
            UserId = user.Id,
            DealerId = dealer.Id,
            DealerName = dealer.BusinessName ?? dealer.TradeName ?? "Dealer",
            Role = invitation.DealerRole.ToString(),
            Message = isNewUser
                ? "¡Bienvenido! Tu cuenta ha sido creada y ya eres parte del equipo."
                : "¡Excelente! Ya eres parte del equipo.",
            IsNewUser = isNewUser
        };
    }
}
