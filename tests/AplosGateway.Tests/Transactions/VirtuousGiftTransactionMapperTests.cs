using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;
using Xunit;

namespace AplosGateway.Tests.Transactions;

public sealed class VirtuousGiftTransactionMapperTests
{
    [Fact]
    public void Map_CreatesExpectedBalancedAplosTransaction()
    {
        var mapper =
            new VirtuousGiftTransactionMapper();

        var gift =
            new VirtuousGift
            {
                Id = 12345,
                ContactName = "Example Donor",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        8,
                        28,
                        14,
                        30,
                        0,
                        DateTimeKind.Utc),
                Amount = 125.50m,
                Project = "Unrestricted Giving",
                ProjectCode = "41025",
                Segment = "2026 Unrestricted Giving Annual Fund"
            };

        var result =
            mapper.Map(gift);

        Assert.Equal(
            "Virtuous Gift 12345 - Example Donor",
            result.Note);

        Assert.Equal(
            "2026-08-28",
            result.Date);

        Assert.Equal(
            "Virtuous",
            result.Contact.CompanyName);

        Assert.Equal(
            "company",
            result.Contact.Type);

        Assert.Equal(
            2,
            result.Lines.Count);

        var depositLine =
            result.Lines[0];

        Assert.Equal(
            125.50m,
            depositLine.Amount);

        Assert.Equal(
            20114,
            depositLine.Account.AccountNumber);

        Assert.Equal(
            492387,
            depositLine.Fund.Id);

        var incomeLine =
            result.Lines[1];

        Assert.Equal(
            -125.50m,
            incomeLine.Amount);

        Assert.Equal(
            41025,
            incomeLine.Account.AccountNumber);

        Assert.Equal(
            492387,
            incomeLine.Fund.Id);

        Assert.Equal(
            0m,
            result.Lines.Sum(
                line => line.Amount));
    }

    [Fact]
    public void Map_ThrowsWhenGiftIsNull()
    {
        var mapper =
            new VirtuousGiftTransactionMapper();

        Assert.Throws<ArgumentNullException>(
            () => mapper.Map(null!));
    }

    [Fact]
    public void Map_ThrowsWhenGiftIdIsInvalid()
    {
        var mapper =
            new VirtuousGiftTransactionMapper();

        var gift =
            new VirtuousGift
            {
                Id = 0,
                ContactName = "Example Donor",
                GiftDateUtc = DateTime.UtcNow,
                Amount = 100m
            };

        Assert.Throws<ArgumentException>(
            () => mapper.Map(gift));
    }

    [Fact]
    public void Map_ThrowsWhenAmountIsInvalid()
    {
        var mapper =
            new VirtuousGiftTransactionMapper();

        var gift =
            new VirtuousGift
            {
                Id = 12345,
                ContactName = "Example Donor",
                GiftDateUtc = DateTime.UtcNow,
                Amount = 0m
            };

        Assert.Throws<ArgumentException>(
            () => mapper.Map(gift));
    }
}