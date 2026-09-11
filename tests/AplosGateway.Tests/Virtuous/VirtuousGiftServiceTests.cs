using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;
using AplosGateway.Infrastructure.Virtuous;

namespace AplosGateway.Tests.Virtuous;

public sealed class VirtuousGiftServiceTests
{
    [Fact]
    public async Task ProcessGiftAsync_FirstAttemptFails_AllowsRetry()
    {
        var expectedTransaction =
            new AplosTransactionRequest
            {
                Note = "Mapped transaction"
            };

        var mapper =
            new StubMapper(expectedTransaction);

        var transactionService =
            new FailingThenSuccessfulTransactionService();

        var idempotencyStore =
            new InMemoryVirtuousGiftIdempotencyStore();

        var responseParser =
            new AplosTransactionResponseParser();

        var service =
            new VirtuousGiftService(
                mapper,
                transactionService,
                idempotencyStore,
                responseParser);

        var gift =
            new VirtuousGift
            {
                Id = 12345,
                ContactName = "Ray Test",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        8,
                        28,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 1.00m
            };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                service.ProcessGiftAsync(gift));

        var result =
            await service.ProcessGiftAsync(gift);

        Assert.Equal(
            "processed",
            result.Status);

        Assert.Equal(
            12345,
            result.GiftId);

        Assert.Equal(
            70064235,
            result.AplosTransactionId);

        Assert.Equal(
            2,
            transactionService.CallCount);
    }

    [Fact]
    public async Task ProcessGiftAsync_MapsGift_AndCreatesTransaction()
    {
        var expectedTransaction =
            new AplosTransactionRequest
            {
                Note = "Mapped transaction"
            };

        var mapper =
            new StubMapper(expectedTransaction);

        var transactionService =
            new StubTransactionService(
                """
                {
                  "status": 200,
                  "data": {
                    "transaction": {
                      "id": 70064235
                    }
                  }
                }
                """);

        var idempotencyStore =
            new InMemoryVirtuousGiftIdempotencyStore();

        var responseParser =
            new AplosTransactionResponseParser();

        var service =
            new VirtuousGiftService(
                mapper,
                transactionService,
                idempotencyStore,
                responseParser);

        var gift =
            new VirtuousGift
            {
                Id = 12345,
                ContactName = "Ray Test",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        8,
                        28,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 1.00m
            };

        var result =
            await service.ProcessGiftAsync(gift);

        Assert.Equal(
            "processed",
            result.Status);

        Assert.Equal(
            12345,
            result.GiftId);

        Assert.Equal(
            70064235,
            result.AplosTransactionId);

        Assert.Same(
            gift,
            mapper.LastGift);

        Assert.Same(
            expectedTransaction,
            transactionService.LastRequest);

        Assert.Equal(
            1,
            transactionService.CallCount);
    }

    [Fact]
    public async Task ProcessGiftAsync_SameGiftIdTwice_CreatesTransactionOnlyOnce()
    {
        var expectedTransaction =
            new AplosTransactionRequest
            {
                Note = "Mapped transaction"
            };

        var mapper =
            new StubMapper(expectedTransaction);

        var transactionService =
            new StubTransactionService(
                """
                {
                  "status": 200,
                  "data": {
                    "transaction": {
                      "id": 70064235
                    }
                  }
                }
                """);

        var idempotencyStore =
            new InMemoryVirtuousGiftIdempotencyStore();

        var responseParser =
            new AplosTransactionResponseParser();

        var service =
            new VirtuousGiftService(
                mapper,
                transactionService,
                idempotencyStore,
                responseParser);

        var firstGift =
            new VirtuousGift
            {
                Id = 12345,
                ContactName = "Ray Test",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        8,
                        28,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 1.00m
            };

        var duplicateGift =
            new VirtuousGift
            {
                Id = 12345,
                ContactName = "Ray Test",
                GiftDateUtc =
                    new DateTime(
                        2026,
                        8,
                        28,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                Amount = 1.00m
            };

        var firstResult =
            await service.ProcessGiftAsync(firstGift);

        var secondResult =
            await service.ProcessGiftAsync(duplicateGift);

        Assert.Equal(
            firstResult.Status,
            secondResult.Status);

        Assert.Equal(
            firstResult.GiftId,
            secondResult.GiftId);

        Assert.Equal(
            firstResult.AplosTransactionId,
            secondResult.AplosTransactionId);

        Assert.Equal(
            1,
            transactionService.CallCount);

        Assert.Equal(
            1,
            mapper.CallCount);
    }

    [Fact]
    public async Task ProcessGiftAsync_NullGift_ThrowsArgumentNullException()
    {
        var mapper =
            new StubMapper(
                new AplosTransactionRequest());

        var transactionService =
            new StubTransactionService(
                """
                {
                  "status": 200,
                  "data": {
                    "transaction": {
                      "id": 70064235
                    }
                  }
                }
                """);

        var idempotencyStore =
            new InMemoryVirtuousGiftIdempotencyStore();

        var responseParser =
            new AplosTransactionResponseParser();

        var service =
            new VirtuousGiftService(
                mapper,
                transactionService,
                idempotencyStore,
                responseParser);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                service.ProcessGiftAsync(
                    null!));
    }

    private sealed class StubMapper
        : IVirtuousGiftTransactionMapper
    {
        private readonly AplosTransactionRequest _transaction;

        public StubMapper(
            AplosTransactionRequest transaction)
        {
            _transaction = transaction;
        }

        public VirtuousGift? LastGift { get; private set; }

        public int CallCount { get; private set; }

        public AplosTransactionRequest Map(
            VirtuousGift gift)
        {
            LastGift = gift;
            CallCount++;

            return _transaction;
        }
    }

    private sealed class StubTransactionService
        : IAplosTransactionService
    {
        private readonly string _result;

        public StubTransactionService(
            string result)
        {
            _result = result;
        }

        public AplosTransactionRequest? LastRequest { get; private set; }

        public int CallCount { get; private set; }

        public Task<string> CreateTransactionAsync(
            AplosTransactionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            CallCount++;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class FailingThenSuccessfulTransactionService
        : IAplosTransactionService
    {
        public int CallCount { get; private set; }

        public Task<string> CreateTransactionAsync(
            AplosTransactionRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (CallCount == 1)
            {
                throw new InvalidOperationException(
                    "Simulated Aplos failure.");
            }

            return Task.FromResult(
                """
                {
                  "status": 200,
                  "data": {
                    "transaction": {
                      "id": 70064235
                    }
                  }
                }
                """);
        }
    }
}