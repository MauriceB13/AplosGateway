namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousWebhookMapper
    : IVirtuousWebhookMapper
{
    private const long SupportedOrganizationId = 7472;

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

        if (request.Organization.Id != SupportedOrganizationId)
        {
            throw new InvalidOperationException(
                $"Unsupported Virtuous organization " +
                $"'{request.Organization.Id}'.");
        }

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

 var designations =
    request.Gift.GiftDesignations
    ?? [];

if (designations.Count > 1)
{
    throw new InvalidOperationException(
        "Virtuous gifts with multiple designations " +
        "are not currently supported.");
}

var designation =
    designations.FirstOrDefault();

if (designation is not null)
{
    if (designation.GiftId != request.Gift.Id)
    {
        throw new InvalidOperationException(
            $"Virtuous designation gift ID " +
            $"'{designation.GiftId}' does not match " +
            $"gift ID '{request.Gift.Id}'.");
    }

    if (designation.AmountDesignated != request.Gift.Amount)
    {
        throw new InvalidOperationException(
            $"Virtuous designation amount " +
            $"'{designation.AmountDesignated}' does not match " +
            $"gift amount '{request.Gift.Amount}'.");
    }
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