using AplosGateway.Core.Virtuous;

namespace AplosGateway.Core.Transactions;

public sealed class VirtuousGiftTransactionMapper
    : IVirtuousGiftTransactionMapper
{
    private const int DepositAccountNumber = 20114;
    private const int IncomeAccountNumber = 41025;
    private const int FundId = 492387;

    public AplosTransactionRequest Map(
        VirtuousGift gift)
    {
        ArgumentNullException.ThrowIfNull(gift);

        if (gift.Id <= 0)
        {
            throw new ArgumentException(
                "Virtuous gift ID must be greater than zero.",
                nameof(gift));
        }

        if (gift.Amount <= 0)
        {
            throw new ArgumentException(
                "Virtuous gift amount must be greater than zero.",
                nameof(gift));
        }

        return new AplosTransactionRequest
        {
            Note =
                $"Virtuous Gift {gift.Id} - {gift.ContactName}",

            Date =
                gift.GiftDateUtc.ToString("yyyy-MM-dd"),

            Contact =
                new AplosTransactionContact
                {
                    CompanyName = "Virtuous",
                    Type = "company"
                },

            Lines =
            [
                new AplosTransactionLine
                {
                    Amount = gift.Amount,

                    Account =
                        new AplosTransactionAccount
                        {
                            AccountNumber =
                                DepositAccountNumber
                        },

                    Fund =
                        new AplosFund
                        {
                            Id = FundId
                        }
                },

                new AplosTransactionLine
                {
                    Amount = -gift.Amount,

                    Account =
                        new AplosTransactionAccount
                        {
                            AccountNumber =
                                IncomeAccountNumber
                        },

                    Fund =
                        new AplosFund
                        {
                            Id = FundId
                        }
                }
            ]
        };
    }
}