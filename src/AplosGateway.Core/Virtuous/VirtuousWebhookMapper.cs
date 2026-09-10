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

        var designation =
            request.Gift.GiftDesignations
                .FirstOrDefault();

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