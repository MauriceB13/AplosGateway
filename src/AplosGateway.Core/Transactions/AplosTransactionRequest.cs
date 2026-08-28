using System.Text.Json.Serialization;

namespace AplosGateway.Core.Transactions;

public sealed class AplosTransactionRequest
{
    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("contact")]
    public AplosTransactionContact Contact { get; set; } = new();

    [JsonPropertyName("lines")]
    public List<AplosTransactionLine> Lines { get; set; } = new();
}

public sealed class AplosTransactionContact
{
    [JsonPropertyName("companyname")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "company";
}

public sealed class AplosTransactionLine
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("account")]
    public AplosTransactionAccount Account { get; set; } = new();

    [JsonPropertyName("fund")]
    public AplosFund Fund { get; set; } = new();
}

public sealed class AplosTransactionAccount
{
    [JsonPropertyName("account_number")]
    public int AccountNumber { get; set; }
}

public sealed class AplosFund
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}