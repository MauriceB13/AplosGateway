using System.Net;
using System.Net.Http;
using System.Text;
using AplosGateway.Core.Configuration;
using AplosGateway.Core.Security;
using AplosGateway.Infrastructure.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace AplosGateway.Tests.Authentication;

public sealed class AplosAuthenticationServiceTests
{
    [Fact]
    public async Task GetAccessTokenAsync_RequestsDecryptsAndCachesToken()
    {
        const string encryptedToken = "encrypted-token";
        const string decryptedToken = "decrypted-token";

        var handler = new StubHttpMessageHandler(
            request =>
            {
                Assert.Equal(
                    "https://app.aplos.com/hermes/api/v1/auth/test-client-id",
                    request.RequestUri?.ToString());

                var response =
                    new HttpResponseMessage(HttpStatusCode.OK);

                response.Content = new StringContent(
                    """
                    {
                      "version": "1",
                      "status": "success",
                      "data": {
                        "expires": "2099-01-01T00:00:00Z",
                        "token": "encrypted-token"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json");

                return response;
            });

        using var httpClient = new HttpClient(handler);

        var decryptor = new StubTokenDecryptor(
            encryptedToken,
            "test-private-key",
            decryptedToken);

        using var cache = new MemoryCache(
            new MemoryCacheOptions());

        var options = Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1",
                ClientId = "test-client-id",
                PrivateKey = "test-private-key"
            });

        var service = new AplosAuthenticationService(
            httpClient,
            decryptor,
            cache,
            options);

        var firstResult =
            await service.GetAccessTokenAsync();

        var secondResult =
            await service.GetAccessTokenAsync();

        Assert.Equal(decryptedToken, firstResult);
        Assert.Equal(decryptedToken, secondResult);

        Assert.Equal(1, handler.CallCount);
        Assert.Equal(1, decryptor.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ThrowsWhenClientIdIsMissing()
    {
        var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK));

        using var httpClient = new HttpClient(handler);

        var decryptor = new StubTokenDecryptor(
            "encrypted-token",
            "private-key",
            "decrypted-token");

        using var cache = new MemoryCache(
            new MemoryCacheOptions());

        var options = Options.Create(
            new AplosOptions
            {
                BaseUrl =
                    "https://app.aplos.com/hermes/api/v1",
                ClientId = "",
                PrivateKey = "private-key"
            });

        var service = new AplosAuthenticationService(
            httpClient,
            decryptor,
            cache,
            options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetAccessTokenAsync());
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

    private sealed class StubTokenDecryptor
        : IAplosTokenDecryptor
    {
        private readonly string _expectedEncryptedToken;
        private readonly string _expectedPrivateKey;
        private readonly string _result;

        public int CallCount { get; private set; }

        public StubTokenDecryptor(
            string expectedEncryptedToken,
            string expectedPrivateKey,
            string result)
        {
            _expectedEncryptedToken =
                expectedEncryptedToken;

            _expectedPrivateKey =
                expectedPrivateKey;

            _result = result;
        }

        public string Decrypt(
            string encryptedToken,
            string privateKey)
        {
            CallCount++;

            Assert.Equal(
                _expectedEncryptedToken,
                encryptedToken);

            Assert.Equal(
                _expectedPrivateKey,
                privateKey);

            return _result;
        }
    }
}