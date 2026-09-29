namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGiftProcessingRecord
{
    public long GiftId { get; init; }

    public string? GiftFingerprint { get; init; }

    public Guid AttemptId { get; init; }

    public VirtuousGiftProcessingStatus Status { get; init; }

    public long? AplosTransactionId { get; init; }

    public string? AplosResponse { get; init; }

    public int AttemptCount { get; init; }

    public DateTime CreatedUtc { get; init; }

    public DateTime? LastAttemptUtc { get; init; }

    public DateTime? CompletedUtc { get; init; }

    public string? FailureMessage { get; init; }
}