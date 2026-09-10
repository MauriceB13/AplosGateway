namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousWebhookMapper
    : IVirtuousWebhookMapper
{
    public VirtuousGift Map(
        VirtuousGiftWebhookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Gift);

        if (!string.Equals(
                request.Event,
                "GiftCreate",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported Virtuous event '{request.Event}'.");
        }

        if (request.Gift.GiftDesignations.Count > 1)
        {
            throw new InvalidOperationException(
                "Virtuous gifts with multiple designations " +
                "are not currently supported.");
        }

        var designation =
            request.Gift.GiftDesignations
                .FirstOrDefault();

        if (request.Gift.Id <= 0)
        {
            throw new InvalidOperationException(
                "Virtuous gift ID must be greater than zero.");
        }

        if (request.Gift.Amount <= 0)
        {
            throw new InvalidOperationException(
                "Virtuous gift amount must be greater than zero.");
        }

        if (request.Gift.GiftDateUtc == default)
        {
            throw new InvalidOperationException(
                "Virtuous gift date is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Gift.ContactName))
        {
            throw new InvalidOperationException(
                "Virtuous contact name is required.");
        }

        if (!string.IsNullOrWhiteSpace(
                request.Gift.CurrencyCode)
            &&
            !string.Equals(
                request.Gift.CurrencyCode,
                "USD",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported Virtuous currency " +
                $"'{request.Gift.CurrencyCode}'.");
        }

        return new VirtuousGift
        {
            Id = request.Gift.Id,

            ContactName =
                request.Gift.ContactName,

            GiftDateUtc =
                request.Gift.GiftDateUtc,

            Amount =
                request.Gift.Amount,

            Project =
                designation?.Project
                ?? string.Empty,

            ProjectCode =
                designation?.ProjectCode
                ?? string.Empty,

            Segment =
                request.Gift.Segment
        };
    }
}