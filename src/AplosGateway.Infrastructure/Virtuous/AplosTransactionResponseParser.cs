using System.Text.Json;
using AplosGateway.Core.Virtuous;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class AplosTransactionResponseParser
{
    public VirtuousGiftProcessingResult Parse(
        long giftId,
        string aplosResponse)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId));
        }

        if (string.IsNullOrWhiteSpace(aplosResponse))
        {
            throw new InvalidOperationException(
                "Aplos transaction response was empty.");
        }

        using var document =
            JsonDocument.Parse(aplosResponse);

        if (!document.RootElement.TryGetProperty(
                "data",
                out var dataElement)
            ||
            !dataElement.TryGetProperty(
                "transaction",
                out var transactionElement)
            ||
            !transactionElement.TryGetProperty(
                "id",
                out var idElement)
            ||
           !idElement.TryGetInt64(
                out var transactionId)
            ||
            transactionId <= 0)
        {
            throw new InvalidOperationException(
                "Aplos transaction response did not contain a transaction ID.");
        }

        return new VirtuousGiftProcessingResult
        {
            Status = "processed",
            GiftId = giftId,
            AplosTransactionId = transactionId
        };
    }
}