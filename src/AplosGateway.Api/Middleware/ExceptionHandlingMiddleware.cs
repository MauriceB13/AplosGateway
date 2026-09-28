using System.Net;
using System.Text.Json;
using AplosGateway.Core.Virtuous;

namespace AplosGateway.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(
            context,
            exception);
        }
    }

    private static async Task HandleExceptionAsync(
    HttpContext context,
    Exception exception)
{
    if (context.Response.HasStarted)
    {
        return;
    }

    context.Response.Clear();
    context.Response.ContentType = "application/json";

    object response;

    if (exception is VirtuousGiftProcessingStateException processingException)
    {
        context.Response.StatusCode =
            (int)HttpStatusCode.Conflict;

        response = new
        {
            error = processingException.Message,
            giftId = processingException.GiftId,
            processingStatus =
                processingException.Status.ToString(),
            traceId = context.TraceIdentifier
        };
    }
    else
    {
        context.Response.StatusCode =
            (int)HttpStatusCode.InternalServerError;

        response = new
        {
            error = "An unexpected error occurred.",
            traceId = context.TraceIdentifier
        };
    }

    await context.Response.WriteAsync(
        JsonSerializer.Serialize(response));
}

}
