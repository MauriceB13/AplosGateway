using AplosGateway.Core.Configuration;
using AplosGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;

namespace AplosGateway.Tests.Virtuous;

public sealed class SqliteVirtuousGiftIdempotencyStoreTests
{
    [Fact]
    public async Task GetOrAddAsync_PersistsResultAcrossStoreInstances()
    {
        var databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"aplosgateway-test-{Guid.NewGuid():N}.db");

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

            var firstStore =
                new SqliteVirtuousGiftIdempotencyStore(
                    options);

            var firstOperationCallCount = 0;

            var firstResult =
                await firstStore.GetOrAddAsync(
                    12345,
                    () =>
                    {
                        firstOperationCallCount++;

                        return Task.FromResult(
                            """{"status":200,"transactionId":70064235}""");
                    });

            var secondStore =
                new SqliteVirtuousGiftIdempotencyStore(
                    options);

            var secondOperationCallCount = 0;

            var secondResult =
                await secondStore.GetOrAddAsync(
                    12345,
                    () =>
                    {
                        secondOperationCallCount++;

                        return Task.FromResult(
                            """{"status":200,"transactionId":99999999}""");
                    });

            Assert.Equal(
                """{"status":200,"transactionId":70064235}""",
                firstResult);

            Assert.Equal(
                firstResult,
                secondResult);

            Assert.Equal(
                1,
                firstOperationCallCount);

            Assert.Equal(
                0,
                secondOperationCallCount);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task GetOrAddAsync_FailedOperation_IsNotPersisted()
    {
        var databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"aplosgateway-test-{Guid.NewGuid():N}.db");

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

            var firstStore =
                new SqliteVirtuousGiftIdempotencyStore(
                    options);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    firstStore.GetOrAddAsync(
                        12345,
                        () =>
                            throw new InvalidOperationException(
                                "Simulated failure.")));

            var secondStore =
                new SqliteVirtuousGiftIdempotencyStore(
                    options);

            var operationCallCount = 0;

            var result =
                await secondStore.GetOrAddAsync(
                    12345,
                    () =>
                    {
                        operationCallCount++;

                        return Task.FromResult(
                            """{"status":200}""");
                    });

            Assert.Equal(
                """{"status":200}""",
                result);

            Assert.Equal(
                1,
                operationCallCount);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}