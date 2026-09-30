using VirtuousGateway.Core.Virtuous;

namespace VirtuousGateway.Core.Transactions;

public interface IVirtuousGiftTransactionMapper
{
    AplosTransactionRequest Map(
        VirtuousGift gift);
}