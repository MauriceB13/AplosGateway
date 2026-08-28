using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class VirtuousGiftService
    : IVirtuousGiftService
{
    private readonly IVirtuousGiftTransactionMapper _mapper;
    private readonly IAplosTransactionService _transactionService;

    public VirtuousGiftService(
        IVirtuousGiftTransactionMapper mapper,
        IAplosTransactionService transactionService)
    {
        _mapper = mapper;
        _transactionService = transactionService;
    }

    public async Task<string> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gift);

        var transaction =
            _mapper.Map(gift);

        return await _transactionService.CreateTransactionAsync(
            transaction,
            cancellationToken);
    }
}