using AplosGateway.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AplosGateway.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    private const string AuthorizationHeader = "Authorization";

    private readonly RequestDelegate _next;
    private readonly SecurityOptions _securityOptions;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> securityOptions)
    {
        _next = next;
        _securityOptions = securityOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(
                AuthorizationHeader,
                out var authorizationHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing authorization header."
            });
            return;
        }

        var expectedValue = $"Bearer {_securityOptions.ApiKey}";

        if (!string.Equals(
                authorizationHeader.ToString(),
                expectedValue,
                StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid API key."
            });
            return;
        }

        await _next(context);
    }
}