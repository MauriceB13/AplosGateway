using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class VirtuousGiftService
    : IVirtuousGiftService
{
    private readonly IVirtuousGiftTransactionMapper _mapper;
    private readonly IAplosTransactionService _transactionService;
    private readonly IVirtuousGiftIdempotencyStore _idempotencyStore;
    private readonly AplosTransactionResponseParser _responseParser;

    public VirtuousGiftService(
        IVirtuousGiftTransactionMapper mapper,
        IAplosTransactionService transactionService,
        IVirtuousGiftIdempotencyStore idempotencyStore,
        AplosTransactionResponseParser responseParser)
    {
        _mapper = mapper;
        _transactionService = transactionService;
        _idempotencyStore = idempotencyStore;
        _responseParser = responseParser;
    }

    public async Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gift);

        var rawResult =
            await _idempotencyStore.GetOrAddAsync(
                gift.Id,
                async () =>
                {
                    var transaction =
                        _mapper.Map(gift);

                    return await _transactionService.CreateTransactionAsync(
                        transaction,
                        cancellationToken);
                });

        return _responseParser.Parse(
            gift.Id,
            rawResult);
    }
}