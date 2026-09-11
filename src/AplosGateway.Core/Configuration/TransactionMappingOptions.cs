namespace AplosGateway.Core.Configuration;

public sealed class TransactionMappingOptions
{
    public const string SectionName =
        "TransactionMapping";

    public int DepositAccountNumber { get; set; }

    public int IncomeAccountNumber { get; set; }

    public int FundId { get; set; }
}