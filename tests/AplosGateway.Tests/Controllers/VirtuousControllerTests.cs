using AplosGateway.Api.Controllers;
using AplosGateway.Core.Transactions;
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

        var mapper =
            new StubVirtuousGiftTransactionMapper();

        var controller =
            new VirtuousController(
                giftService,
                mapper);

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

        Assert.Equal(
            0,
            mapper.CallCount);
    }

    [Fact]
    public void PreviewGift_ReturnsMappedTransactionWithoutProcessingGift()
    {
        var giftService =
            new StubVirtuousGiftService();

        var expectedTransaction =
            new AplosTransactionRequest
            {
                Note = "Preview transaction"
            };

        var mapper =
            new StubVirtuousGiftTransactionMapper(
                expectedTransaction);

        var controller =
            new VirtuousController(
                giftService,
                mapper);

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
            controller.PreviewGift(gift);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var transaction =
            Assert.IsType<AplosTransactionRequest>(
                okResult.Value);

        Assert.Same(
            expectedTransaction,
            transaction);

        Assert.Equal(
            1,
            mapper.CallCount);

        Assert.Same(
            gift,
            mapper.LastGift);

        Assert.Equal(
            0,
            giftService.CallCount);
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

    private sealed class StubVirtuousGiftTransactionMapper
        : IVirtuousGiftTransactionMapper
    {
        private readonly AplosTransactionRequest _transaction;

        public int CallCount { get; private set; }

        public VirtuousGift? LastGift { get; private set; }

        public StubVirtuousGiftTransactionMapper(
            AplosTransactionRequest? transaction = null)
        {
            _transaction =
                transaction ??
                new AplosTransactionRequest();
        }

        public AplosTransactionRequest Map(
            VirtuousGift gift)
        {
            CallCount++;

            LastGift = gift;

            return _transaction;
        }
    }
}