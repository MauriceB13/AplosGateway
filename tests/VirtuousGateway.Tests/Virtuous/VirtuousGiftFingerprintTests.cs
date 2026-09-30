using VirtuousGateway.Core.Virtuous;

namespace VirtuousGateway.Tests.Virtuous;

public sealed class VirtuousGiftFingerprintTests
{
    [Fact]
    public void Create_SameGift_ProducesSameFingerprint()
    {
        var first =
            CreateGift();

        var second =
            CreateGift();

        var firstFingerprint =
            VirtuousGiftFingerprint.Create(
                first);

        var secondFingerprint =
            VirtuousGiftFingerprint.Create(
                second);

        Assert.Equal(
            firstFingerprint,
            secondFingerprint);
    }

    [Fact]
    public void Create_UnspecifiedAndUtcSameClockValue_ProduceSameFingerprint()
    {
        var utcGift =
            CreateGift();

        utcGift.GiftDateUtc =
            new DateTime(
                2026,
                6,
                16,
                12,
                30,
                45,
                DateTimeKind.Utc);

        var unspecifiedGift =
            CreateGift();

        unspecifiedGift.GiftDateUtc =
            new DateTime(
                2026,
                6,
                16,
                12,
                30,
                45,
                DateTimeKind.Unspecified);

        Assert.Equal(
            VirtuousGiftFingerprint.Create(
                utcGift),
            VirtuousGiftFingerprint.Create(
                unspecifiedGift));
    }

    [Fact]
    public void Create_ChangedAmount_ProducesDifferentFingerprint()
    {
        var first =
            CreateGift();

        var second =
            CreateGift();

        second.Amount =
            first.Amount + 1m;

        Assert.NotEqual(
            VirtuousGiftFingerprint.Create(
                first),
            VirtuousGiftFingerprint.Create(
                second));
    }

    [Fact]
    public void Create_ChangedSourceField_ProducesDifferentFingerprint()
    {
        var first =
            CreateGift();

        var second =
            CreateGift();

        second.ProjectCode =
            first.ProjectCode + "-CHANGED";

        Assert.NotEqual(
            VirtuousGiftFingerprint.Create(
                first),
            VirtuousGiftFingerprint.Create(
                second));
    }

    [Fact]
    public void Create_EmbeddedFieldDelimiters_DoNotCreateAmbiguousFingerprint()
    {
        var first =
            CreateGift();

        first.ContactName =
            "Alpha\nProject=Beta";

        first.Project =
            "Gamma";

        var second =
            CreateGift();

        second.ContactName =
            "Alpha";

        second.Project =
            "Beta\nProject=Gamma";

        Assert.NotEqual(
            VirtuousGiftFingerprint.Create(
                first),
            VirtuousGiftFingerprint.Create(
                second));
    }

    [Fact]
    public void Create_ReturnsSha256HexFingerprint()
    {
        var gift =
            CreateGift();

        var fingerprint =
            VirtuousGiftFingerprint.Create(
                gift);

        Assert.Equal(
            64,
            fingerprint.Length);

        Assert.Matches(
            "^[0-9A-F]{64}$",
            fingerprint);
    }

    private static VirtuousGift CreateGift()
    {
        return new VirtuousGift
        {
            Id = 12345,
            ContactName = "Test Contact",
            GiftDateUtc =
                new DateTime(
                    2026,
                    6,
                    16,
                    12,
                    30,
                    45,
                    DateTimeKind.Utc),
            Amount = 125.50m,
            Project = "Unrestricted Giving",
            ProjectCode = "41025",
            Segment = "Annual Fund"
        };
    }
}