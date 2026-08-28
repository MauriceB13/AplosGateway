using System.Net;
using System.Net.Http;
using AplosGateway.Core.Authentication;
using AplosGateway.Core.Configuration;
using AplosGateway.Infrastructure.Aplos;
using Microsoft.Extensions.Options;
using Xunit;

namespace AplosGateway.Tests.Aplos;

public sealed class AplosApiClientTests
{
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

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"status":"ok"}""")
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
            _ => new HttpResponseMessage(
                HttpStatusCode.OK));

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

    private sealed class StubHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            HttpResponseMessage> _handler;

        public int CallCount { get; private set; }

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(
                _handler(request));
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
}