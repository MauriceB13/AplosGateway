using AplosGateway.Core.Configuration;
using AplosGateway.Core.Virtuous;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class PostgresVirtuousGiftIdempotencyStore
    : IVirtuousGiftIdempotencyStore
{
    private readonly string _connectionString;

    public PostgresVirtuousGiftIdempotencyStore(
        IOptions<IdempotencyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

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

        await EnsureDatabaseAsync();

        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await AcquireGiftLockAsync(
                connection,
                transaction,
                giftId);

            var existingResult =
                await GetExistingResultAsync(
                    connection,
                    transaction,
                    giftId);

            if (existingResult is not null)
            {
                await transaction.CommitAsync();

                return existingResult;
            }

            var result =
                await operation();

            await SaveResultAsync(
                connection,
                transaction,
                giftId,
                result);

            await transaction.CommitAsync();

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }

    private async Task EnsureDatabaseAsync()
    {
        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS virtuous_gift_idempotency
            (
                gift_id BIGINT NOT NULL PRIMARY KEY,
                result TEXT NOT NULL,
                created_utc TIMESTAMPTZ NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task AcquireGiftLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long giftId)
    {
        await using var command =
            connection.CreateCommand();

        command.Transaction =
            transaction;

        command.CommandText =
            """
            SELECT pg_advisory_xact_lock(@giftId);
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> GetExistingResultAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long giftId)
    {
        await using var command =
            connection.CreateCommand();

        command.Transaction =
            transaction;

        command.CommandText =
            """
            SELECT result
            FROM virtuous_gift_idempotency
            WHERE gift_id = @giftId;
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        var result =
            await command.ExecuteScalarAsync();

        return result as string;
    }

    private static async Task SaveResultAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long giftId,
        string result)
    {
        await using var command =
            connection.CreateCommand();

        command.Transaction =
            transaction;

        command.CommandText =
            """
            INSERT INTO virtuous_gift_idempotency
                (gift_id, result, created_utc)
            VALUES
                (@giftId, @result, @createdUtc);
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        command.Parameters.AddWithValue(
            "result",
            result);

        command.Parameters.AddWithValue(
            "createdUtc",
            DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }
}