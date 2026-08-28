namespace AplosGateway.Api.Configuration;

public sealed class AplosOptions
{
    public const string SectionName = "Aplos";

    public string BaseUrl { get; set; } = "https://app.aplos.com/hermes/api/v1";

    public string ClientId { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;
}