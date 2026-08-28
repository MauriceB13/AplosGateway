using AplosGateway.Core.Virtuous;

namespace AplosGateway.Core.Transactions;

public interface IVirtuousGiftTransactionMapper
{
    AplosTransactionRequest Map(
        VirtuousGift gift);
}