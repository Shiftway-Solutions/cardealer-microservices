using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

public class TwoFactorAuth
{
    public string Id { get; private set; }
    public string UserId { get; private set; }
    public TwoFactorAuthType PrimaryMethod { get; private set; }
    public TwoFactorAuthStatus Status { get; private set; }
    public string Secret { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public List<string> RecoveryCodes { get; private set; } = new();
    public int FailedAttempts { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Métodos secundarios habilitados
    public List<TwoFactorAuthType> EnabledMethods { get; private set; } = new();

    // Navigation property (relación 1:1)
    public virtual ApplicationUser User { get; private set; } = null!;

    // CORRECCIÓN: Constructor privado inicializa todas las propiedades requeridas
    private TwoFactorAuth()
    {
        Id = Guid.NewGuid().ToString();
        UserId = string.Empty; // Inicializar con valor por defecto
        PrimaryMethod = TwoFactorAuthType.Authenticator; // Valor por defecto
        Status = TwoFactorAuthStatus.Disabled; // Valor por defecto
        CreatedAt = DateTime.UtcNow;
    }

    public TwoFactorAuth(string userId, TwoFactorAuthType primaryMethod, string? phoneNumber = null)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        PrimaryMethod = primaryMethod;
        Status = TwoFactorAuthStatus.PendingVerification;
        PhoneNumber = phoneNumber;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Setup 2FA with secret and recovery codes but keep in PendingVerification status.
    /// User must verify with a code before 2FA is fully enabled.
    /// </summary>
    public void SetupPending(string secret, List<string> recoveryCodes)
    {
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        RecoveryCodes = recoveryCodes ?? throw new ArgumentNullException(nameof(recoveryCodes));
        Status = TwoFactorAuthStatus.PendingVerification;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Complete 2FA setup after user verifies with a valid code.
    /// Changes status from PendingVerification to Enabled.
    /// </summary>
    public void ConfirmEnable(List<TwoFactorAuthType>? enabledMethods = null)
    {
        if (Status != TwoFactorAuthStatus.PendingVerification)
            throw new InvalidOperationException("2FA must be in PendingVerification status to confirm");

        EnabledMethods = enabledMethods ?? new List<TwoFactorAuthType> { PrimaryMethod };
        Status = TwoFactorAuthStatus.Enabled;
        EnabledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable(string secret, List<string> recoveryCodes, List<TwoFactorAuthType>? enabledMethods = null)
    {
        if (Status == TwoFactorAuthStatus.Enabled)
            throw new InvalidOperationException("2FA is already enabled");

        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        RecoveryCodes = recoveryCodes ?? throw new ArgumentNullException(nameof(recoveryCodes));
        EnabledMethods = enabledMethods ?? new List<TwoFactorAuthType> { PrimaryMethod };
        Status = TwoFactorAuthStatus.Enabled;
        EnabledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePrimaryMethod(TwoFactorAuthType newMethod, string? newSecret = null, string? newPhoneNumber = null)
    {
        if (Status != TwoFactorAuthStatus.Enabled)
            throw new InvalidOperationException("2FA must be enabled to change method");

        PrimaryMethod = newMethod;

        if (!string.IsNullOrEmpty(newSecret))
            Secret = newSecret;

        if (!string.IsNullOrEmpty(newPhoneNumber))
            PhoneNumber = newPhoneNumber;

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSecondaryMethod(TwoFactorAuthType method)
    {
        if (!EnabledMethods.Contains(method))
        {
            EnabledMethods.Add(method);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveSecondaryMethod(TwoFactorAuthType method)
    {
        if (method == PrimaryMethod)
            throw new InvalidOperationException("Cannot remove primary method");

        if (EnabledMethods.Contains(method))
        {
            EnabledMethods.Remove(method);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsMethodEnabled(TwoFactorAuthType method) => EnabledMethods.Contains(method);

    public void Disable()
    {
        Status = TwoFactorAuthStatus.Disabled;
        Secret = string.Empty;
        RecoveryCodes.Clear();
        EnabledMethods.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementFailedAttempts()
    {
        FailedAttempts++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetFailedAttempts()
    {
        FailedAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool UseRecoveryCode(string code)
    {
        if (RecoveryCodes.Contains(code))
        {
            RecoveryCodes.Remove(code);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates recovery codes with a new set.
    /// Called when user exhausts all codes and system auto-regenerates.
    /// </summary>
    public void UpdateRecoveryCodes(List<string> newCodes)
    {
        RecoveryCodes = newCodes ?? throw new ArgumentNullException(nameof(newCodes));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets authenticator with a new secret and recovery codes.
    /// Called when user loses their authenticator device and uses all recovery codes.
    /// </summary>
    public void ResetAuthenticator(string newSecret, List<string> newRecoveryCodes)
    {
        if (PrimaryMethod != TwoFactorAuthType.Authenticator)
            throw new InvalidOperationException("Cannot reset authenticator for non-authenticator 2FA");

        Secret = newSecret ?? throw new ArgumentNullException(nameof(newSecret));
        RecoveryCodes = newRecoveryCodes ?? throw new ArgumentNullException(nameof(newRecoveryCodes));
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasRecoveryCodes => RecoveryCodes.Any();
}
