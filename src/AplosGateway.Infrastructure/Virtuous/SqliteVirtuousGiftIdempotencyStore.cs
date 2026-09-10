using System.Collections.Concurrent;
using AplosGateway.Core.Configuration;
using AplosGateway.Core.Virtuous;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class SqliteVirtuousGiftIdempotencyStore
    : IVirtuousGiftIdempotencyStore
{
    private readonly string _connectionString;

    private readonly ConcurrentDictionary<long, SemaphoreSlim>
        _giftLocks = new();

    public SqliteVirtuousGiftIdempotencyStore(
        IOptions<IdempotencyOptions> options)
    {
        _connectionString =
            options.Value.ConnectionString;
    }

    public async Task<string> GetOrAddAsync(
        long giftId,
        Func<Task<string>> operation)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId),
                "Gift ID must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(operation);

        var giftLock =
            _giftLocks.GetOrAdd(
                giftId,
                _ => new SemaphoreSlim(1, 1));

        await giftLock.WaitAsync();

        try
        {
            await EnsureDatabaseAsync();

            var existingResult =
                await GetExistingResultAsync(giftId);

            if (existingResult is not null)
            {
                return existingResult;
            }

            var result =
                await operation();

            await SaveResultAsync(
                giftId,
                result);

            return result;
        }
        finally
        {
            giftLock.Release();
        }
    }

    private async Task EnsureDatabaseAsync()
    {
        EnsureDatabaseDirectoryExists();

        await using var connection =
            new SqliteConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS VirtuousGiftIdempotency
            (
                GiftId INTEGER NOT NULL PRIMARY KEY,
                Result TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> GetExistingResultAsync(
        long giftId)
    {
        await using var connection =
            new SqliteConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT Result
            FROM VirtuousGiftIdempotency
            WHERE GiftId = $giftId;
            """;

        command.Parameters.AddWithValue(
            "$giftId",
            giftId);

        var result =
            await command.ExecuteScalarAsync();

        return result as string;
    }

    private async Task SaveResultAsync(
        long giftId,
        string result)
    {
        await using var connection =
            new SqliteConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO VirtuousGiftIdempotency
                (GiftId, Result, CreatedUtc)
            VALUES
                ($giftId, $result, $createdUtc);
            """;

        command.Parameters.AddWithValue(
            "$giftId",
            giftId);

        command.Parameters.AddWithValue(
            "$result",
            result);

        command.Parameters.AddWithValue(
            "$createdUtc",
            DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    private void EnsureDatabaseDirectoryExists()
    {
        var builder =
            new SqliteConnectionStringBuilder(
                _connectionString);

        if (string.IsNullOrWhiteSpace(
                builder.DataSource))
        {
            return;
        }

        var directory =
            Path.GetDirectoryName(
                builder.DataSource);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}