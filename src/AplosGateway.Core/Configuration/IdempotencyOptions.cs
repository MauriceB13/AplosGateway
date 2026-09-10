namespace AplosGateway.Core.Configuration;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public string ConnectionString { get; set; } =
        "Data Source=Data/aplosgateway.db";
}