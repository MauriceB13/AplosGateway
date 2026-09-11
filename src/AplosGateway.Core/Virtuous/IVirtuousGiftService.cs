namespace AplosGateway.Core.Virtuous;

public interface IVirtuousGiftService
{
    Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default);
}