using AplosGateway.Core.Configuration;
using AplosGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AplosGateway.Tests.Virtuous;

[Collection("PostgreSQL Integration")]
public sealed class PostgresVirtuousGiftIdempotencyStoreTests
{
    private static string ConnectionString =>
    Environment.GetEnvironmentVariable(
        "Idempotency__ConnectionString")
    ?? throw new InvalidOperationException(
        "Idempotency__ConnectionString must be configured to run PostgreSQL integration tests.");

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task GetOrAddAsync_PersistsResultAcrossStoreInstances()
    {
        await ClearDatabaseAsync();

        var options =
            Options.Create(
                new IdempotencyOptions
                {
                    ConnectionString =
                        ConnectionString
                });

        var firstStore =
            new PostgresVirtuousGiftIdempotencyStore(
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
            new PostgresVirtuousGiftIdempotencyStore(
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

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
public async Task GetOrAddAsync_ConcurrentStoreInstances_ExecutesOperationOnce()
{
    await ClearDatabaseAsync();

    var options =
        Options.Create(
            new IdempotencyOptions
            {
                ConnectionString =
                    ConnectionString
            });

    var firstStore =
        new PostgresVirtuousGiftIdempotencyStore(
            options);

    var secondStore =
        new PostgresVirtuousGiftIdempotencyStore(
            options);

    var operationCallCount = 0;

    var operationStarted =
        new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

    var releaseOperation =
        new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

    async Task<string> Operation()
    {
        Interlocked.Increment(
            ref operationCallCount);

        operationStarted.TrySetResult(true);

        await releaseOperation.Task;

        return """{"status":200,"transactionId":70064235}""";
    }

    var firstTask =
        firstStore.GetOrAddAsync(
            12345,
            Operation);

    await operationStarted.Task;

    var secondTask =
        secondStore.GetOrAddAsync(
            12345,
            Operation);

    await Task.Delay(250);

    Assert.Equal(
        1,
        Volatile.Read(
            ref operationCallCount));

    releaseOperation.SetResult(true);

    var results =
        await Task.WhenAll(
            firstTask,
            secondTask);

    Assert.Equal(
        1,
        Volatile.Read(
            ref operationCallCount));

    Assert.All(
        results,
        result =>
            Assert.Equal(
                """{"status":200,"transactionId":70064235}""",
                result));
}

[Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task GetOrAddAsync_FailedOperation_IsNotPersisted()
{
    await ClearDatabaseAsync();

    var options =
        Options.Create(
            new IdempotencyOptions
            {
                ConnectionString =
                    ConnectionString
            });

    var firstStore =
        new PostgresVirtuousGiftIdempotencyStore(
            options);

    var firstOperationCallCount = 0;

    await Assert.ThrowsAsync<InvalidOperationException>(
        () =>
            firstStore.GetOrAddAsync(
                12345,
                () =>
                {
                    firstOperationCallCount++;

                    throw new InvalidOperationException(
                        "Simulated operation failure.");
                }));

    var secondStore =
        new PostgresVirtuousGiftIdempotencyStore(
            options);

    var secondOperationCallCount = 0;

    var result =
        await secondStore.GetOrAddAsync(
            12345,
            () =>
            {
                secondOperationCallCount++;

                return Task.FromResult(
                    """{"status":200,"transactionId":70064235}""");
            });

    Assert.Equal(
        1,
        firstOperationCallCount);

    Assert.Equal(
        1,
        secondOperationCallCount);

    Assert.Equal(
        """{"status":200,"transactionId":70064235}""",
        result);
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
            DROP TABLE IF EXISTS virtuous_gift_idempotency;
            """;

        await command.ExecuteNonQueryAsync();
    }
}