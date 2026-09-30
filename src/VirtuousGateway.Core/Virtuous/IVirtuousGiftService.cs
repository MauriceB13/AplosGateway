namespace VirtuousGateway.Core.Virtuous;

public interface IVirtuousGiftService
{
    Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default);

    Task<VirtuousGiftProcessingResult> RetryFailedGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default);
}