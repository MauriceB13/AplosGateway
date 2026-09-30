using System.Net;
using System.Text.Json;
using VirtuousGateway.Api.Middleware;
using VirtuousGateway.Core.Virtuous;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace VirtuousGateway.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ProcessingStateException_ReturnsConflict()
    {
        const long giftId = 38118;

        var context = CreateHttpContext();

        var middleware =
            new ExceptionHandlingMiddleware(
                _ => throw new VirtuousGiftProcessingStateException(
                    giftId,
                    VirtuousGiftProcessingStatus.RequiresReconciliation),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(
            (int)HttpStatusCode.Conflict,
            context.Response.StatusCode);

        Assert.Equal(
            "application/json",
            context.Response.ContentType);

        var responseBody =
            await ReadResponseBodyAsync(context);

        using var document =
            JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        Assert.Equal(
            giftId,
            root.GetProperty("giftId").GetInt64());

        Assert.Equal(
            "RequiresReconciliation",
            root.GetProperty("processingStatus").GetString());

        Assert.Contains(
            "RequiresReconciliation",
            root.GetProperty("error").GetString());

        Assert.Equal(
            context.TraceIdentifier,
            root.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_ReturnsSanitizedInternalServerError()
    {
        const string sensitiveMessage =
            "Sensitive internal failure details.";

        var context = CreateHttpContext();

        var middleware =
            new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException(
                    sensitiveMessage),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(
            (int)HttpStatusCode.InternalServerError,
            context.Response.StatusCode);

        Assert.Equal(
            "application/json",
            context.Response.ContentType);

        var responseBody =
            await ReadResponseBodyAsync(context);

        Assert.DoesNotContain(
            sensitiveMessage,
            responseBody);

        using var document =
            JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        Assert.Equal(
            "An unexpected error occurred.",
            root.GetProperty("error").GetString());

        Assert.Equal(
            context.TraceIdentifier,
            root.GetProperty("traceId").GetString());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context =
            new DefaultHttpContext();

        context.Response.Body =
            new MemoryStream();

        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(
        HttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader =
            new StreamReader(
                context.Response.Body,
                leaveOpen: true);

        return await reader.ReadToEndAsync();
    }
}