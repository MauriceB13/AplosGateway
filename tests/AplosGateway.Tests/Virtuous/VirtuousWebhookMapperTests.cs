using AplosGateway.Core.Virtuous;

namespace AplosGateway.Tests.Virtuous;

public sealed class VirtuousWebhookMapperTests
{
        [Fact]
    public void Map_InvalidGiftId_ThrowsInvalidOperationException()
    {
        var request =
            CreateValidRequest();

        request.Gift.Id = 0;

        var mapper =
            new VirtuousWebhookMapper();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => mapper.Map(request));

        Assert.Contains(
            "gift ID",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_NonPositiveAmount_ThrowsInvalidOperationException()
    {
        var request =
            CreateValidRequest();

        request.Gift.Amount = 0m;

        var mapper =
            new VirtuousWebhookMapper();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => mapper.Map(request));

        Assert.Contains(
            "amount",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_MissingGiftDate_ThrowsInvalidOperationException()
    {
        var request =
            CreateValidRequest();

        request.Gift.GiftDateUtc = default;

        var mapper =
            new VirtuousWebhookMapper();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => mapper.Map(request));

        Assert.Contains(
            "gift date",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_BlankContactName_ThrowsInvalidOperationException()
    {
        var request =
            CreateValidRequest();

        request.Gift.ContactName = " ";

        var mapper =
            new VirtuousWebhookMapper();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => mapper.Map(request));

        Assert.Contains(
            "contact name",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_UnsupportedCurrency_ThrowsInvalidOperationException()
    {
        var request =
            CreateValidRequest();

        request.Gift.CurrencyCode = "CAD";

        var mapper =
            new VirtuousWebhookMapper();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => mapper.Map(request));

        Assert.Contains(
            "Unsupported Virtuous currency",
            exception.Message);
    }
    
    [Fact]
    public void Map_ValidWebhook_MapsGiftAndFirstDesignation()
    {
        var request =
            new VirtuousGiftWebhookRequest
            {
                Event = "GiftCreate",
                EventId =
                    "6b2be4f5-b67b-4808-a800-9fadef0c5d01",

                Gift =
                    new VirtuousWebhookGift
                    {
                        Id = 38241,
                        ContactId = 6020,
                        ContactName = "John Smith",
                        GiftType = "Check",

                        GiftDateUtc =
                            new DateTime(
                                2026,
                                9,
                                2,
                                0,
                                0,
                                0,
                                DateTimeKind.Utc),

                        Amount = 150m,
                        CurrencyCode = "USD",
                        CheckNumber = "1000",

                        Segment =
                            "2026 Golf Scramble Web All Contacts ",

                        SegmentCode =
                            "2026GSWBAC",

                        State = "ME",

                        GiftDesignations =
                        [
                            new VirtuousGiftDesignation
                            {
                                Id = 38414,
                                GiftId = 38241,
                                ProjectId = 104,

                                Project =
                                    "Reunion Golf Hole Sponsor",

                                ProjectCode =
                                    "49999-25",

                                AmountDesignated = 150m
                            }
                        ]
                    }
            };

        var mapper =
            new VirtuousWebhookMapper();

        var result =
            mapper.Map(request);

        Assert.Equal(
            38241,
            result.Id);

        Assert.Equal(
            "John Smith",
            result.ContactName);

        Assert.Equal(
            new DateTime(
                2026,
                9,
                2,
                0,
                0,
                0,
                DateTimeKind.Utc),
            result.GiftDateUtc);

        Assert.Equal(
            150m,
            result.Amount);

        Assert.Equal(
            "Reunion Golf Hole Sponsor",
            result.Project);

        Assert.Equal(
            "49999-25",
            result.ProjectCode);

        Assert.Equal(
            "2026 Golf Scramble Web All Contacts ",
            result.Segment);
    }

    [Fact]
    public void Map_NoDesignations_UsesEmptyProjectValues()
    {
        var request =
    new VirtuousGiftWebhookRequest
    {
        Event = "GiftCreate",

        Gift =
            new VirtuousWebhookGift
            {
                Id = 38241,
                ContactName = "John Smith",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        9,
                        2,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 150m
            }
    };

        var mapper =
            new VirtuousWebhookMapper();

        var result =
            mapper.Map(request);

        Assert.Equal(
            string.Empty,
            result.Project);

        Assert.Equal(
            string.Empty,
            result.ProjectCode);
    }

    [Fact]
public void Map_MultipleDesignations_ThrowsInvalidOperationException()
{
    var request =
        new VirtuousGiftWebhookRequest
        {
            Event = "GiftCreate",

            Gift =
                new VirtuousWebhookGift
                {
                    Id = 38241,
                    ContactName = "John Smith",

                    GiftDateUtc =
                        new DateTime(
                            2026,
                            9,
                            2,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),

                    Amount = 150m,

                    GiftDesignations =
                    [
                        new VirtuousGiftDesignation
                        {
                            Id = 1,
                            GiftId = 38241,
                            ProjectId = 101,
                            Project = "First Project",
                            ProjectCode = "10001",
                            AmountDesignated = 100m
                        },

                        new VirtuousGiftDesignation
                        {
                            Id = 2,
                            GiftId = 38241,
                            ProjectId = 102,
                            Project = "Second Project",
                            ProjectCode = "10002",
                            AmountDesignated = 50m
                        }
                    ]
                }
        };

    var mapper =
    new VirtuousWebhookMapper();

    var exception =
        Assert.Throws<InvalidOperationException>(
            () => mapper.Map(request));

    Assert.Contains(
        "multiple designations",
        exception.Message,
        StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_UnsupportedEvent_ThrowsInvalidOperationException()
    {
    var request =
        new VirtuousGiftWebhookRequest
        {
            Event = "GiftUpdate",

            Gift =
                new VirtuousWebhookGift
                {
                    Id = 38241,
                    ContactName = "John Smith",
                    GiftDateUtc =
                        new DateTime(
                            2026,
                            9,
                            2,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),
                    Amount = 150m
                }
        };

    var mapper =
        new VirtuousWebhookMapper();

    var exception =
        Assert.Throws<InvalidOperationException>(
            () =>
                mapper.Map(request));

    Assert.Contains(
        "Unsupported Virtuous event",
        exception.Message);
}

    [Fact]
    public void Map_NullRequest_ThrowsArgumentNullException()
    {
        var mapper =
            new VirtuousWebhookMapper();

        Assert.Throws<ArgumentNullException>(
            () =>
                mapper.Map(null!));
    }

    private static VirtuousGiftWebhookRequest
    CreateValidRequest()
{
    return new VirtuousGiftWebhookRequest
    {
        Event = "GiftCreate",

        EventId =
            "6b2be4f5-b67b-4808-a800-9fadef0c5d01",

        EventDateTimeUtc =
            new DateTime(
                2026,
                9,
                3,
                16,
                13,
                7,
                DateTimeKind.Utc),

        Gift =
            new VirtuousWebhookGift
            {
                Id = 38241,
                ContactId = 6020,
                ContactName = "John Smith",
                GiftType = "Check",

                GiftDateUtc =
                    new DateTime(
                        2026,
                        9,
                        2,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),

                Amount = 150m,
                CurrencyCode = "USD",
                CheckNumber = "1000",

                Segment =
                    "2026 Golf Scramble Web All Contacts ",

                SegmentCode =
                    "2026GSWBAC",

                State = "ME",

                GiftDesignations =
                [
                    new VirtuousGiftDesignation
                    {
                        Id = 38414,
                        GiftId = 38241,
                        ProjectId = 104,

                        Project =
                            "Reunion Golf Hole Sponsor",

                        ProjectCode =
                            "49999-25",

                        AmountDesignated = 150m
                    }
                ]
            }
    };
}
}