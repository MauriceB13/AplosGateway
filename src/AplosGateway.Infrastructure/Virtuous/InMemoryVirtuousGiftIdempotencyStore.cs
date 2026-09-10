using System.Collections.Concurrent;
using AplosGateway.Core.Virtuous;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class InMemoryVirtuousGiftIdempotencyStore
    : IVirtuousGiftIdempotencyStore
{
    private readonly ConcurrentDictionary<
        long,
        Lazy<Task<string>>> _entries = new();

    public async Task<string> GetOrAddAsync(
        long giftId,
        Func<Task<string>> operation)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId),
                "Gift ID must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(operation);

        var lazyOperation =
            _entries.GetOrAdd(
                giftId,
                _ =>
                    new Lazy<Task<string>>(
                        operation,
                        LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazyOperation.Value;
        }
        catch
        {
            _entries.TryRemove(
                new KeyValuePair<long, Lazy<Task<string>>>(
                    giftId,
                    lazyOperation));

            throw;
        }
    }
}