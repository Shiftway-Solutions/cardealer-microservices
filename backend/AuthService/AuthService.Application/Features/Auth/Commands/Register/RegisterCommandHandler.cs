// AuthService.Application/UseCases/Register/RegisterCommandHandler.cs
using AuthService.Shared.Exceptions;
using AuthService.Domain.Entities;
using RefreshTokenEntity = AuthService.Domain.Entities.RefreshToken;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;
using AuthService.Domain.Enums;
using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Interfaces;
using CarDealer.Contracts.Events.Auth;
using AuthService.Application.Common.Interfaces;

namespace AuthService.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IVerificationTokenRepository _verificationTokenRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRequestContext _requestContext;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IVerificationTokenRepository verificationTokenRepository,
        IAuthNotificationService notificationService,
        IEventPublisher eventPublisher,
        IRequestContext requestContext)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _verificationTokenRepository = verificationTokenRepository;
        _notificationService = notificationService;
        _eventPublisher = eventPublisher;
        _requestContext = requestContext;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            throw new ConflictException("User with this email already exists.");

        // Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // Create user - Generate a valid username for Identity (no spaces allowed)
        // UserName is used for Identity, DisplayName for UI purposes
        var displayName = request.GetDisplayName();
        var userName = !string.IsNullOrWhiteSpace(request.UserName)
            ? request.UserName.Trim().Replace(" ", "")
            : request.Email.Split('@')[0]; // Use email prefix as username

        var user = new ApplicationUser(userName, request.Email, passwordHash);

        // Store FirstName, LastName on the entity
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();

        // Set AccountType and UserIntent from request (default: Buyer / Browse)
        // Buyer, Seller, and Dealer can register directly.
        // Admin and PlatformEmployee require special onboarding flows.
        user.AccountType = request.AccountType switch
        {
            Domain.Enums.AccountType.Seller => Domain.Enums.AccountType.Seller,
            Domain.Enums.AccountType.Dealer => Domain.Enums.AccountType.Dealer,
            _ => Domain.Enums.AccountType.Buyer
        };

        // Auto-set UserIntent based on AccountType if caller didn't override it
        user.UserIntent = request.UserIntent != Domain.Enums.UserIntent.Browse
            ? request.UserIntent
            : user.AccountType == Domain.Enums.AccountType.Seller
                ? Domain.Enums.UserIntent.Sell
                : Domain.Enums.UserIntent.Browse;

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            user.PhoneNumber = request.Phone;
        }

        await _userRepository.AddAsync(user, cancellationToken);

        // Generate tokens
        var accessToken = _jwtGenerator.GenerateToken(user);
        var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        // Save refresh token
        var refreshTokenEntity = new RefreshTokenEntity(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(7),
            _requestContext.IpAddress
        );

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        // Create verification token and send email
        var verificationToken = new VerificationToken(
            user.Id,
            VerificationTokenType.EmailVerification,
            TimeSpan.FromHours(24)
        );

        await _verificationTokenRepository.AddAsync(verificationToken);

        // CORRECCIÓN: Usar operador de supresión nula (!) ya que sabemos que no son nulos
        // IMPORTANTE: Solo enviamos email de VERIFICACIÓN aquí
        // El email de bienvenida se enviará DESPUÉS de que el usuario verifique su email
        await _notificationService.SendEmailConfirmationAsync(user.Email!, verificationToken.Token);

        // Parse FirstName and LastName from request or UserName
        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;

        // If FirstName/LastName not provided, try to parse from UserName
        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(request.UserName))
        {
            var nameParts = request.UserName.Split(' ', 2);
            firstName = nameParts.Length > 0 ? nameParts[0] : request.UserName;
            lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        }

        // Publish UserRegisteredEvent with all user data for sync with UserService
        var userRegisteredEvent = new UserRegisteredEvent
        {
            UserId = Guid.Parse(user.Id),
            Email = user.Email!,
            FullName = request.GetDisplayName(), // Uses FirstName LastName or UserName
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = request.Phone,
            RegisteredAt = user.CreatedAt,
            Metadata = new Dictionary<string, string>
            {
                { "AccountType", ((int)user.AccountType).ToString() }
            }
        };
        await _eventPublisher.PublishAsync(userRegisteredEvent, cancellationToken);

        // CORRECCIÓN: Usar operador de supresión nula (!) para evitar warnings
        return new RegisterResponse(
            user.Id,
            user.UserName!,
            user.Email!,
            accessToken,
            refreshTokenValue,
            expiresAt
        );
    }
}
