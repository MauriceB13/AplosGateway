using AplosGateway.Core.Configuration;

namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousWebhookMapper
    : IVirtuousWebhookMapper
{
    private readonly VirtuousOptions _options;

    public VirtuousWebhookMapper(
        VirtuousOptions options)
    {
        _options = options;
    }

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
            throw new VirtuousWebhookValidationException(
                $"Unsupported Virtuous event '{request.Event}'.");
        }

        if (request.Organization.Id != _options.OrganizationId)
        {
            throw new VirtuousWebhookValidationException(
                $"Unsupported Virtuous organization " +
                $"'{request.Organization.Id}'.");
        }

        if (request.Gift.Id <= 0)
        {
            throw new VirtuousWebhookValidationException(
                "Virtuous gift ID must be greater than zero.");
        }

        if (request.Gift.Amount <= 0)
        {
            throw new VirtuousWebhookValidationException(
                "Virtuous gift amount must be greater than zero.");
        }

        if (request.Gift.GiftDateUtc == default)
        {
            throw new VirtuousWebhookValidationException(
                "Virtuous gift date is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Gift.ContactName))
        {
            throw new VirtuousWebhookValidationException(
                "Virtuous contact name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Gift.CurrencyCode))
        {
            throw new VirtuousWebhookValidationException(
                "Virtuous gift currency is required.");
        }

        if (!string.Equals(
                request.Gift.CurrencyCode,
                "USD",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new VirtuousWebhookValidationException(
                $"Unsupported Virtuous currency " +
                $"'{request.Gift.CurrencyCode}'.");
        }

        var designations =
            request.Gift.GiftDesignations
            ?? [];

        if (designations.Count > 1)
        {
            throw new VirtuousWebhookValidationException(
                "Virtuous gifts with multiple designations " +
                "are not currently supported.");
        }

        var designation =
            designations.FirstOrDefault();

        if (designation is not null)
        {
            if (designation.GiftId != request.Gift.Id)
            {
                throw new VirtuousWebhookValidationException(
                    $"Virtuous designation gift ID " +
                    $"'{designation.GiftId}' does not match " +
                    $"gift ID '{request.Gift.Id}'.");
            }

            if (designation.AmountDesignated != request.Gift.Amount)
            {
                throw new VirtuousWebhookValidationException(
                    $"Virtuous designation amount " +
                    $"'{designation.AmountDesignated}' does not match " +
                    $"gift amount '{request.Gift.Amount}'.");
            }
        }

        return new VirtuousGift
        {
            Id = request.Gift.Id,
            ContactName = request.Gift.ContactName,
            GiftDateUtc = request.Gift.GiftDateUtc,
            Amount = request.Gift.Amount,

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