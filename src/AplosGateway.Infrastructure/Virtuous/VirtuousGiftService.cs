using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class VirtuousGiftService
    : IVirtuousGiftService
{
    private readonly IVirtuousGiftTransactionMapper _mapper;
    private readonly IAplosTransactionService _transactionService;
    private readonly IVirtuousGiftIdempotencyStore _idempotencyStore;

    public VirtuousGiftService(
        IVirtuousGiftTransactionMapper mapper,
        IAplosTransactionService transactionService,
        IVirtuousGiftIdempotencyStore idempotencyStore)
    {
        _mapper = mapper;
        _transactionService = transactionService;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<string> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gift);

        return await _idempotencyStore.GetOrAddAsync(
            gift.Id,
            async () =>
            {
                var transaction =
                    _mapper.Map(gift);

                return await _transactionService.CreateTransactionAsync(
                    transaction,
                    cancellationToken);
            });
    }
}