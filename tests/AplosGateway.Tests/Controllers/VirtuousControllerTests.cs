using AplosGateway.Api.Controllers;
using AplosGateway.Core.Virtuous;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AplosGateway.Tests.Controllers;

public sealed class VirtuousControllerTests
{
    [Fact]
    public async Task ProcessGift_ReturnsAplosJsonResponse()
    {
        var giftService =
            new StubVirtuousGiftService();

        var controller =
            new VirtuousController(giftService);

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
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 125.50m
            };

        var result =
            await controller.ProcessGift(
                gift,
                CancellationToken.None);

        var contentResult =
            Assert.IsType<ContentResult>(result);

        Assert.Equal(
            """{"status":"created"}""",
            contentResult.Content);

        Assert.Equal(
            "application/json",
            contentResult.ContentType);

        Assert.Equal(
            1,
            giftService.CallCount);

        Assert.Same(
            gift,
            giftService.LastGift);
    }

    private sealed class StubVirtuousGiftService
        : IVirtuousGiftService
    {
        public int CallCount { get; private set; }

        public VirtuousGift? LastGift { get; private set; }

        public Task<string> ProcessGiftAsync(
            VirtuousGift gift,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            LastGift = gift;

            return Task.FromResult(
                """{"status":"created"}""");
        }
    }
}