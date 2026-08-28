using System.Net.Http.Json;
using AplosGateway.Core.Configuration;
using AplosGateway.Core.Authentication;
using AplosGateway.Core.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AplosGateway.Infrastructure.Authentication;

public sealed class AplosAuthenticationService
    : IAplosAuthenticationService
{
    private const string CacheKey = "AplosAccessToken";

    private readonly HttpClient _httpClient;
    private readonly IAplosTokenDecryptor _tokenDecryptor;
    private readonly IMemoryCache _cache;
    private readonly AplosOptions _options;

    public AplosAuthenticationService(
        HttpClient httpClient,
        IAplosTokenDecryptor tokenDecryptor,
        IMemoryCache cache,
        IOptions<AplosOptions> options)
    {
        _httpClient = httpClient;
        _tokenDecryptor = tokenDecryptor;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<string>(CacheKey, out var cachedToken) &&
            !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        ValidateConfiguration();

        var requestUri =
            $"{_options.BaseUrl.TrimEnd('/')}/auth/{Uri.EscapeDataString(_options.ClientId)}";

        using var response = await _httpClient.GetAsync(
            requestUri,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var encryptedToken = await response.Content.ReadFromJsonAsync<string>(
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(encryptedToken))
        {
            throw new InvalidOperationException(
                "Aplos returned an empty authentication token.");
        }

        var accessToken = _tokenDecryptor.Decrypt(
            encryptedToken,
            _options.PrivateKey);

        _cache.Set(
            CacheKey,
            accessToken,
            TimeSpan.FromMinutes(25));

        return accessToken;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException(
                "Aplos ClientId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            throw new InvalidOperationException(
                "Aplos PrivateKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException(
                "Aplos BaseUrl is not configured.");
        }
    }
}