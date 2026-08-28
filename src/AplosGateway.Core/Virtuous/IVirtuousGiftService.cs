namespace AplosGateway.Core.Virtuous;

public interface IVirtuousGiftService
{
    Task<string> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default);
}