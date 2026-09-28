using System.Net;
using System.Net.Http;
using AplosGateway.Core.Authentication;
using AplosGateway.Core.Configuration;
using AplosGateway.Infrastructure.Aplos;
using Microsoft.Extensions.Options;
using Xunit;
using AplosGateway.Core.Aplos;

namespace AplosGateway.Tests.Aplos;

public sealed class AplosApiClientTests
{

[Fact]
public async Task PostAsync_AuthenticationFailure_ThrowsNotDispatched()
{
    var authenticationService =
        new FailingAuthenticationService();

    var handler =
        new StubHttpMessageHandler(
            _ =>
                Task.FromResult(
                    new HttpResponseMessage(
                        HttpStatusCode.OK)));

    var httpClient =
        new HttpClient(handler);

    var options =
        Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

    var client =
        new AplosApiClient(
            httpClient,
            authenticationService,
            options);

    var exception =
        await Assert.ThrowsAsync<AplosPostNotDispatchedException>(
            () =>
                client.PostAsync(
                    "transactions",
                    """{"test":true}"""));

    Assert.Contains(
        "not dispatched",
        exception.Message,
        StringComparison.OrdinalIgnoreCase);

    Assert.Equal(
        0,
        handler.CallCount);
}

[Fact]
public async Task PostAsync_ResponseBodyReadFailure_ThrowsOutcomeUnknown()
{
    var handler =
        new StubHttpMessageHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    Content =
                        new FailingHttpContent()
                }));

    using var httpClient =
        new HttpClient(handler);

    var authenticationService =
        new StubAplosAuthenticationService(
            "test-access-token");

    var options =
        Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

    var client =
        new AplosApiClient(
            httpClient,
            authenticationService,
            options);

    var exception =
        await Assert.ThrowsAsync<AplosPostOutcomeUnknownException>(
            () =>
                client.PostAsync(
                    "transactions",
                    """{"note":"test"}"""));

    Assert.IsType<HttpRequestException>(
        exception.InnerException);

    Assert.Contains(
        "response could not be fully read",
        exception.Message,
        StringComparison.Ordinal);

    Assert.Equal(
        1,
        handler.CallCount);
}

    [Fact]
    public async Task GetAsync_SendsBearerTokenAndReturnsResponse()
    {
        var handler = new StubHttpMessageHandler(
            request =>
            {
                Assert.Equal(
                    HttpMethod.Get,
                    request.Method);

                Assert.Equal(
                    "https://app.aplos.com/hermes/api/v1/test-endpoint",
                    request.RequestUri?.ToString());

                Assert.NotNull(
                    request.Headers.Authorization);

                Assert.Equal(
                    "Bearer",
                    request.Headers.Authorization!.Scheme);

                Assert.Equal(
                    "test-access-token",
                    request.Headers.Authorization.Parameter);

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            """{"status":"ok"}""")
                    });
            });

        using var httpClient =
            new HttpClient(handler);

        var authenticationService =
            new StubAplosAuthenticationService(
                "test-access-token");

        var options = Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

        var client = new AplosApiClient(
            httpClient,
            authenticationService,
            options);

        var result =
            await client.GetAsync(
                "test-endpoint");

        Assert.Equal(
            """{"status":"ok"}""",
            result);

        Assert.Equal(
            1,
            authenticationService.CallCount);

        Assert.Equal(
            1,
            handler.CallCount);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenRelativePathIsEmpty()
    {
        var handler = new StubHttpMessageHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.OK)));

        using var httpClient =
            new HttpClient(handler);

        var authenticationService =
            new StubAplosAuthenticationService(
                "test-access-token");

        var options = Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

        var client = new AplosApiClient(
            httpClient,
            authenticationService,
            options);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetAsync(""));
    }

    [Fact]
    public async Task PostAsync_SendsBearerTokenJsonBodyAndReturnsResponse()
    {
        const string requestJson =
            """
            {
              "note": "test transaction"
            }
            """;

        var handler = new StubHttpMessageHandler(
            async request =>
            {
                Assert.Equal(
                    HttpMethod.Post,
                    request.Method);

                Assert.Equal(
                    "https://app.aplos.com/hermes/api/v1/transactions",
                    request.RequestUri?.ToString());

                Assert.NotNull(
                    request.Headers.Authorization);

                Assert.Equal(
                    "Bearer",
                    request.Headers.Authorization!.Scheme);

                Assert.Equal(
                    "test-access-token",
                    request.Headers.Authorization.Parameter);

                Assert.NotNull(request.Content);

                Assert.Equal(
                    "application/json",
                    request.Content!.Headers.ContentType?.MediaType);

                var body =
                    await request.Content.ReadAsStringAsync();

                Assert.Equal(
                    requestJson,
                    body);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"status":"created"}""")
                };
            });

        using var httpClient =
            new HttpClient(handler);

        var authenticationService =
            new StubAplosAuthenticationService(
                "test-access-token");

        var options = Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

        var client = new AplosApiClient(
            httpClient,
            authenticationService,
            options);

        var result =
            await client.PostAsync(
                "transactions",
                requestJson);

        Assert.Equal(
            """{"status":"created"}""",
            result);

        Assert.Equal(
            1,
            authenticationService.CallCount);

        Assert.Equal(
            1,
            handler.CallCount);
    }

    [Fact]
    public async Task PostAsync_ThrowsWhenJsonContentIsEmpty()
    {
        var handler = new StubHttpMessageHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.OK)));

        using var httpClient =
            new HttpClient(handler);

        var authenticationService =
            new StubAplosAuthenticationService(
                "test-access-token");

        var options = Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

        var client = new AplosApiClient(
            httpClient,
            authenticationService,
            options);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.PostAsync(
                "transactions",
                ""));
    }

    [Fact]
public async Task PostAsync_BadRequest_ThrowsRejectedWithoutExposingResponseBody()
{
    const string sensitiveResponse =
        """
        {
          "error": "Sensitive downstream detail",
          "token": "secret-token-12345"
        }
        """;

    var handler =
        new StubHttpMessageHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.BadRequest)
                {
                    Content =
                        new StringContent(
                            sensitiveResponse)
                }));

    using var httpClient =
        new HttpClient(handler);

    var authenticationService =
        new StubAplosAuthenticationService(
            "test-access-token");

    var options =
        Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

    var client =
        new AplosApiClient(
            httpClient,
            authenticationService,
            options);

    var exception =
        await Assert.ThrowsAsync<AplosPostRejectedException>(
            () =>
                client.PostAsync(
                    "transactions",
                    """{"note":"test"}"""));

    Assert.Equal(
        HttpStatusCode.BadRequest,
        exception.StatusCode);

    Assert.Contains(
        "HTTP 400",
        exception.Message,
        StringComparison.Ordinal);

    Assert.DoesNotContain(
        "Sensitive downstream detail",
        exception.Message,
        StringComparison.OrdinalIgnoreCase);

    Assert.DoesNotContain(
        "secret-token-12345",
        exception.Message,
        StringComparison.OrdinalIgnoreCase);
}

    [Fact]
public async Task PostAsync_ServerError_ThrowsOutcomeUnknown()
{
    var handler =
        new StubHttpMessageHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.InternalServerError)
                {
                    Content =
                        new StringContent(
                            """{"error":"sensitive server detail"}""")
                }));

    using var httpClient =
        new HttpClient(handler);

    var authenticationService =
        new StubAplosAuthenticationService(
            "test-access-token");

    var options =
        Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

    var client =
        new AplosApiClient(
            httpClient,
            authenticationService,
            options);

    var exception =
        await Assert.ThrowsAsync<AplosPostOutcomeUnknownException>(
            () =>
                client.PostAsync(
                    "transactions",
                    """{"note":"test"}"""));

    Assert.Contains(
        "HTTP 500",
        exception.Message,
        StringComparison.Ordinal);

    Assert.DoesNotContain(
        "sensitive server detail",
        exception.Message,
        StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task PostAsync_TransportFailure_ThrowsOutcomeUnknown()
{
    var handler =
        new StubHttpMessageHandler(
            _ => throw new HttpRequestException(
                "Simulated transport failure."));

    using var httpClient =
        new HttpClient(handler);

    var authenticationService =
        new StubAplosAuthenticationService(
            "test-access-token");

    var options =
        Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

    var client =
        new AplosApiClient(
            httpClient,
            authenticationService,
            options);

    var exception =
        await Assert.ThrowsAsync<AplosPostOutcomeUnknownException>(
            () =>
                client.PostAsync(
                    "transactions",
                    """{"note":"test"}"""));

    Assert.IsType<HttpRequestException>(
        exception.InnerException);

    Assert.Equal(
        1,
        handler.CallCount);
}

[Fact]
public async Task PostAsync_CancellationDuringSend_ThrowsOutcomeUnknown()
{
    var handler =
        new StubHttpMessageHandler(
            _ => throw new OperationCanceledException(
                "Simulated cancellation."));

    using var httpClient =
        new HttpClient(handler);

    var authenticationService =
        new StubAplosAuthenticationService(
            "test-access-token");

    var options =
        Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1"
            });

    var client =
        new AplosApiClient(
            httpClient,
            authenticationService,
            options);

    var exception =
        await Assert.ThrowsAsync<AplosPostOutcomeUnknownException>(
            () =>
                client.PostAsync(
                    "transactions",
                    """{"note":"test"}"""));

    Assert.IsType<OperationCanceledException>(
        exception.InnerException);

    Assert.Equal(
        1,
        handler.CallCount);
}

    private sealed class FailingHttpContent
    : HttpContent
{
    protected override Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context)
    {
        return Task.FromException(
            new HttpRequestException(
                "Simulated response body read failure."));
    }

    protected override bool TryComputeLength(
        out long length)
    {
        length = 0;

        return false;
    }
}

    private sealed class StubHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            Task<HttpResponseMessage>> _handler;

        public int CallCount { get; private set; }

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return await _handler(request);
        }
    }

    private sealed class StubAplosAuthenticationService
        : IAplosAuthenticationService
    {
        private readonly string _token;

        public int CallCount { get; private set; }

        public StubAplosAuthenticationService(
            string token)
        {
            _token = token;
        }

        public Task<string> GetAccessTokenAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                _token);
        }
    }

    private sealed class FailingAuthenticationService
    : IAplosAuthenticationService
    {
        public Task<string> GetAccessTokenAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<string>(
                new InvalidOperationException(
                    "Simulated authentication failure."));
        }
    }
}