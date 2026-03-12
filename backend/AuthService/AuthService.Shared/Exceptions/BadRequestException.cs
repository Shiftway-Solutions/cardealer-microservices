namespace AuthService.Shared.Exceptions;

/// <summary>
/// Exception thrown when request validation fails.
/// HTTP 400 Bad Request.
/// </summary>
public class BadRequestException : AuthServiceException
{
    public BadRequestException(string message = "Bad request")
        : base(message, 400, "BAD_REQUEST")
    {
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, 400, "BAD_REQUEST", innerException)
    {
    }
}
