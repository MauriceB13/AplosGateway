using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;
using AplosGateway.Infrastructure.Virtuous;
using Xunit;

namespace AplosGateway.Tests.Virtuous;

public sealed class VirtuousGiftServiceTests
{
    [Fact]
    public async Task ProcessGiftAsync_MapsGiftAndCreatesTransaction()
    {
        var expectedTransaction =
            new AplosTransactionRequest
            {
                Note = "Mapped transaction"
            };

        var mapper =
            new StubVirtuousGiftTransactionMapper(
                expectedTransaction);

        var transactionService =
            new StubAplosTransactionService();

        var service =
            new VirtuousGiftService(
                mapper,
                transactionService);

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
            await service.ProcessGiftAsync(gift);

        Assert.Equal(
            """{"status":"created"}""",
            result);

        Assert.Equal(
            1,
            mapper.CallCount);

        Assert.Same(
            gift,
            mapper.LastGift);

        Assert.Equal(
            1,
            transactionService.CallCount);

        Assert.Same(
            expectedTransaction,
            transactionService.LastRequest);
    }

    [Fact]
    public async Task ProcessGiftAsync_ThrowsWhenGiftIsNull()
    {
        var mapper =
            new StubVirtuousGiftTransactionMapper(
                new AplosTransactionRequest());

        var transactionService =
            new StubAplosTransactionService();

        var service =
            new VirtuousGiftService(
                mapper,
                transactionService);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.ProcessGiftAsync(null!));

        Assert.Equal(
            0,
            mapper.CallCount);

        Assert.Equal(
            0,
            transactionService.CallCount);
    }

    private sealed class StubVirtuousGiftTransactionMapper
        : IVirtuousGiftTransactionMapper
    {
        private readonly AplosTransactionRequest _transaction;

        public int CallCount { get; private set; }

        public VirtuousGift? LastGift { get; private set; }

        public StubVirtuousGiftTransactionMapper(
            AplosTransactionRequest transaction)
        {
            _transaction = transaction;
        }

        public AplosTransactionRequest Map(
            VirtuousGift gift)
        {
            CallCount++;

            LastGift = gift;

            return _transaction;
        }
    }

    private sealed class StubAplosTransactionService
        : IAplosTransactionService
    {
        public int CallCount { get; private set; }

        public AplosTransactionRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<string> CreateTransactionAsync(
            AplosTransactionRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            LastRequest = request;

            return Task.FromResult(
                """{"status":"created"}""");
        }
    }
}