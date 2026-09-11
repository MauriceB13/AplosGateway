namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGiftProcessingResult
{
    public string Status { get; set; } = "processed";

    public long GiftId { get; set; }

    public long AplosTransactionId { get; set; }
}