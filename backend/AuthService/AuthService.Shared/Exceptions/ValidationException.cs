namespace AuthService.Shared.Exceptions;

public class ValidationException : AuthServiceException
{
    public ValidationException(string message)
        : base(message, 400, "VALIDATION_ERROR")
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, 400, "VALIDATION_ERROR", innerException)
    {
    }
}
