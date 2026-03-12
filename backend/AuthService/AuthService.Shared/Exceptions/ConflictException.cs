namespace AuthService.Shared.Exceptions;

/// <summary>
/// Exception thrown when a resource already exists (duplicate).
/// HTTP 409 Conflict.
/// </summary>
public class ConflictException : AuthServiceException
{
    public ConflictException(string message = "Resource already exists")
        : base(message, 409, "CONFLICT")
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, 409, "CONFLICT", innerException)
    {
    }
}
