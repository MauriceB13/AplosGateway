using VirtuousGateway.Core.Configuration;
using VirtuousGateway.Core.Transactions;
using VirtuousGateway.Core.Virtuous;
using VirtuousGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;
using Npgsql;

namespace VirtuousGateway.Tests.Virtuous;

[Collection("PostgreSQL Integration")]
public sealed class VirtuousGiftRestartPersistenceTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable(
            "ProcessingLedger__ConnectionString")
        ?? throw new InvalidOperationException(
            "ProcessingLedger__ConnectionString must be configured to run PostgreSQL integration tests.");

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task ProcessGiftAsync_AfterRestart_DoesNotPostDuplicate()
    {
        await ClearDatabaseAsync();

        var options =
            Options.Create(
                new ProcessingLedgerOptions
                {
                    ConnectionString =
                        ConnectionString
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

        var firstLedger =
        new PostgresVirtuousGiftProcessingLedger(
            options);

        var responseParser =
            new AplosTransactionResponseParser();

        var firstService =
        new VirtuousGiftService(
            firstMapper,
            firstTransactionService,
            firstLedger,
            responseParser);

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
                """
                {
                  "status": 200,
                  "data": {
                    "transaction": {
                      "id": 99999999
                    }
                  }
                }
                """);

        var secondLedger =
        new PostgresVirtuousGiftProcessingLedger(
            options);

        var secondService =
        new VirtuousGiftService(
            secondMapper,
            secondTransactionService,
            secondLedger,
            responseParser);

        var secondResult =
            await secondService.ProcessGiftAsync(
                gift);

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
            70064235,
            secondResult.AplosTransactionId);

        Assert.Equal(
            0,
            secondTransactionService.CallCount);

        Assert.Equal(
            0,
            secondMapper.CallCount);
    }

    private static async Task ClearDatabaseAsync()
    {
        await using var connection =
            new NpgsqlConnection(
                ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
        """
        DROP TABLE IF EXISTS virtuous_gift_processing;
        """;

        await command.ExecuteNonQueryAsync();
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
