namespace AplosGateway.Core.Virtuous;

public interface IVirtuousGiftIdempotencyStore
{
    Task<string> GetOrAddAsync(
        long giftId,
        Func<Task<string>> operation);
}