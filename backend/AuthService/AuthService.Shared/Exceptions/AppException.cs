namespace AuthService.Shared.Exceptions;

public class AppException : AuthServiceException
{
    public AppException(string message)
        : base(message, 500, "APP_ERROR")
    {
    }

    public AppException(string message, Exception innerException)
        : base(message, 500, "APP_ERROR", innerException)
    {
    }
}
