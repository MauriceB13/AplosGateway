namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGiftWebhookRequest
{
    public VirtuousWebhookGift Gift { get; set; } = new();

    public string Event { get; set; } = string.Empty;

    public string EventId { get; set; } = string.Empty;

    public DateTime EventDateTimeUtc { get; set; }

    public VirtuousOrganization Organization { get; set; } = new();

    public VirtuousOrganizationUser OrganizationUser { get; set; } = new();
}

public sealed class VirtuousWebhookGift
{
    public long Id { get; set; }

    public long ContactId { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string GiftType { get; set; } = string.Empty;

    public DateTime GiftDateUtc { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string CheckNumber { get; set; } = string.Empty;

    public string Segment { get; set; } = string.Empty;

    public string SegmentCode { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public List<VirtuousGiftDesignation> GiftDesignations { get; set; } = new();
}

public sealed class VirtuousGiftDesignation
{
    public long Id { get; set; }

    public long GiftId { get; set; }

    public long ProjectId { get; set; }

    public string Project { get; set; } = string.Empty;

    public string ProjectCode { get; set; } = string.Empty;

    public decimal AmountDesignated { get; set; }
}

public sealed class VirtuousOrganization
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class VirtuousOrganizationUser
{
    public long Id { get; set; }

    public string EmailAddress { get; set; } = string.Empty;
}