namespace AplosGateway.Core.Virtuous;

public interface IVirtuousGiftProcessingLedger
{
    Task<VirtuousGiftProcessingClaim> BeginProcessingAsync(
        long giftId,
        CancellationToken cancellationToken = default);

    Task<VirtuousGiftProcessingRecord> CompleteAsync(
        long giftId,
        Guid attemptId,
        long aplosTransactionId,
        string aplosResponse,
        CancellationToken cancellationToken = default);

    Task<VirtuousGiftProcessingRecord> FailAsync(
        long giftId,
        Guid attemptId,
        string failureMessage,
        CancellationToken cancellationToken = default);

    Task<VirtuousGiftProcessingRecord> RequireReconciliationAsync(
        long giftId,
        Guid attemptId,
        string failureMessage,
        CancellationToken cancellationToken = default);
}