namespace AplosGateway.Api.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string Name { get; set; } = "AplosGateway";

    public string Version { get; set; } = "0.1.0";
}
