namespace AuthService.Shared.Exceptions;

/// <summary>
/// Exception thrown when user is forbidden from accessing a resource.
/// HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : AuthServiceException
{
    public ForbiddenException(string message = "Access forbidden")
        : base(message, 403, "FORBIDDEN")
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, 403, "FORBIDDEN", innerException)
    {
    }
}
