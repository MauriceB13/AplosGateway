using System.Net.Http.Headers;
using AplosGateway.Core.Aplos;
using AplosGateway.Core.Authentication;
using AplosGateway.Core.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace AplosGateway.Infrastructure.Aplos;

public sealed class AplosApiClient : IAplosApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAplosAuthenticationService _authenticationService;
    private readonly AplosOptions _options;

    public AplosApiClient(
        HttpClient httpClient,
        IAplosAuthenticationService authenticationService,
        IOptions<AplosOptions> options)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _options = options.Value;
    }

    public async Task<string> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException(
                "Aplos relative path cannot be empty.",
                nameof(relativePath));
        }

        var accessToken =
            await _authenticationService.GetAccessTokenAsync(
                cancellationToken);

        var requestUri =
            $"{_options.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(
            cancellationToken);
    }
    public async Task<string> PostAsync(
    string relativePath,
    string jsonContent,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(relativePath))
    {
        throw new ArgumentException(
            "Aplos relative path cannot be empty.",
            nameof(relativePath));
    }

    if (string.IsNullOrWhiteSpace(jsonContent))
    {
        throw new ArgumentException(
            "Aplos JSON content cannot be empty.",
            nameof(jsonContent));
    }

    var accessToken =
        await _authenticationService.GetAccessTokenAsync(
            cancellationToken);

    var requestUri =
        $"{_options.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

    using var request =
        new HttpRequestMessage(
            HttpMethod.Post,
            requestUri);

    request.Headers.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            accessToken);

    request.Content =
        new StringContent(
            jsonContent,
            Encoding.UTF8,
            "application/json");

    using var response =
        await _httpClient.SendAsync(
            request,
            cancellationToken);

    var responseContent =
        await response.Content.ReadAsStringAsync(
            cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Aplos returned HTTP {(int)response.StatusCode} " +
            $"({response.StatusCode}). Response: {responseContent}");
    }

    return responseContent;
}
}