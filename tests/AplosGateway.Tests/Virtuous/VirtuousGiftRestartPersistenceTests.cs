using AplosGateway.Core.Configuration;
using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;
using AplosGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;

namespace AplosGateway.Tests.Virtuous;

public sealed class VirtuousGiftRestartPersistenceTests
{
    [Fact]
    public async Task ProcessGiftAsync_AfterRestart_DoesNotPostDuplicate()
    {
        var databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"aplosgateway-restart-test-{Guid.NewGuid():N}.db");

        var connectionString =
            $"Data Source={databasePath};Pooling=False";

        try
        {
            var options =
                Options.Create(
                    new IdempotencyOptions
                    {
                        ConnectionString =
                            connectionString
                    });

            var transaction =
                new AplosTransactionRequest
                {
                    Note = "Mapped transaction"
                };

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

            var firstMapper =
                new StubMapper(transaction);

            var firstTransactionService =
                new StubTransactionService(
                    """{"status":200,"transactionId":70064235}""");

            var firstStore =
                new SqliteVirtuousGiftIdempotencyStore(
                    options);

            var firstService =
                new VirtuousGiftService(
                    firstMapper,
                    firstTransactionService,
                    firstStore);

            var firstResult =
                await firstService.ProcessGiftAsync(
                    gift);

            Assert.Equal(
                1,
                firstTransactionService.CallCount);

            var secondMapper =
                new StubMapper(transaction);

            var secondTransactionService =
                new StubTransactionService(
                    """{"status":200,"transactionId":99999999}""");

            var secondStore =
                new SqliteVirtuousGiftIdempotencyStore(
                    options);

            var secondService =
                new VirtuousGiftService(
                    secondMapper,
                    secondTransactionService,
                    secondStore);

            var secondResult =
                await secondService.ProcessGiftAsync(
                    gift);

            Assert.Equal(
                firstResult,
                secondResult);

            Assert.Equal(
                0,
                secondTransactionService.CallCount);

            Assert.Equal(
                0,
                secondMapper.CallCount);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
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

        public int CallCount { get; private set; }

        public AplosTransactionRequest Map(
            VirtuousGift gift)
        {
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

        public int CallCount { get; private set; }

        public Task<string> CreateTransactionAsync(
            AplosTransactionRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                _result);
        }
    }
}