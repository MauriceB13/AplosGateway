namespace AplosGateway.Core.Configuration;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public string ConnectionString { get; set; } = string.Empty;
}
