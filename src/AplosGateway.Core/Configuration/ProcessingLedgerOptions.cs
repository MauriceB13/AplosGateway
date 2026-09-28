namespace AplosGateway.Core.Configuration;

public sealed class ProcessingLedgerOptions
{
    public const string SectionName = "ProcessingLedger";

    public string ConnectionString { get; set; } = string.Empty;
}