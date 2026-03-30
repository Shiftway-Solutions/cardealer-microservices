namespace AdminService.Api.Handlers;

/// <summary>
/// Forwards the Authorization header from the incoming HTTP request
/// to outgoing service-to-service HTTP calls.
/// </summary>
public class AuthHeaderForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthHeaderForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
