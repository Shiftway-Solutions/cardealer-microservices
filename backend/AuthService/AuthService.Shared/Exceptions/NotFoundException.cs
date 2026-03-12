namespace AuthService.Shared.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// HTTP 404 Not Found.
/// </summary>
public class NotFoundException : AuthServiceException
{
    public NotFoundException(string message = "Resource not found")
        : base(message, 404, "NOT_FOUND")
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, 404, "NOT_FOUND", innerException)
    {
    }
}
