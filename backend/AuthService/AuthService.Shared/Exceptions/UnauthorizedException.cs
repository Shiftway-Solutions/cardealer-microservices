namespace AuthService.Shared.Exceptions;

/// <summary>
/// Exception thrown when authentication fails (invalid credentials).
/// HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : AuthServiceException
{
    public UnauthorizedException(string message = "Unauthorized access")
        : base(message, 401, "UNAUTHORIZED")
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, 401, "UNAUTHORIZED", innerException)
    {
    }
}
