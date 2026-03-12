namespace AuthService.Shared.Exceptions;

public class ServiceUnavailableException : AuthServiceException
{
    public ServiceUnavailableException(string message)
        : base(message, 503, "SERVICE_UNAVAILABLE")
    {
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, 503, "SERVICE_UNAVAILABLE", innerException)
    {
    }
}
