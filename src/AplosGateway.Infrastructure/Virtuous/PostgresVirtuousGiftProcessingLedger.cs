using AplosGateway.Core.Configuration;
using AplosGateway.Core.Virtuous;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AplosGateway.Infrastructure.Virtuous;

public sealed class PostgresVirtuousGiftProcessingLedger
    : IVirtuousGiftProcessingLedger
{
    private readonly string _connectionString;

    public PostgresVirtuousGiftProcessingLedger(
        IOptions<ProcessingLedgerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _connectionString =
            options.Value.ConnectionString;
    }

    public async Task<VirtuousGiftProcessingClaim> BeginProcessingAsync(
        long giftId,
        CancellationToken cancellationToken = default)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId),
                "Gift ID must be greater than zero.");
        }

        await EnsureDatabaseAsync(
            cancellationToken);

        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await AcquireGiftLockAsync(
                connection,
                transaction,
                giftId,
                cancellationToken);

            var existingRecord =
                await GetRecordAsync(
                    connection,
                    transaction,
                    giftId,
                    cancellationToken);

            VirtuousGiftProcessingClaim claim;

            if (existingRecord is null)
            {
                claim =
                    await CreateProcessingRecordAsync(
                        connection,
                        transaction,
                        giftId,
                        cancellationToken);
            }

            else
            {
                claim =
                    new VirtuousGiftProcessingClaim
                    {
                        Record = existingRecord,
                        ShouldProcess = false
                    };
            }

            await transaction.CommitAsync(
                cancellationToken);

            return claim;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    public async Task<VirtuousGiftProcessingRecord> CompleteAsync(
        long giftId,
        Guid attemptId,
        long aplosTransactionId,
        string aplosResponse,
        CancellationToken cancellationToken = default)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId),
                "Gift ID must be greater than zero.");
        }

        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Attempt ID cannot be empty.",
                nameof(attemptId));
        }

        if (aplosTransactionId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aplosTransactionId),
                "Aplos transaction ID must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(aplosResponse))
        {
            throw new ArgumentException(
                "Aplos response cannot be empty.",
                nameof(aplosResponse));
        }

        await EnsureDatabaseAsync(
            cancellationToken);

        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        var completedUtc =
            DateTime.UtcNow;

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            UPDATE virtuous_gift_processing
            SET
                status = 'Completed',
                aplos_transaction_id = @aplosTransactionId,
                aplos_response = @aplosResponse,
                completed_utc = @completedUtc,
                failure_message = NULL
            WHERE
                gift_id = @giftId
                AND attempt_id = @attemptId
                AND status = 'Processing'
            RETURNING
                gift_id,
                attempt_id,
                status,
                aplos_transaction_id,
                aplos_response,
                attempt_count,
                created_utc,
                last_attempt_utc,
                completed_utc,
                failure_message;
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        command.Parameters.AddWithValue(
            "attemptId",
            attemptId);

        command.Parameters.AddWithValue(
            "aplosTransactionId",
            aplosTransactionId);

        command.Parameters.AddWithValue(
            "aplosResponse",
            aplosResponse);

        command.Parameters.AddWithValue(
            "completedUtc",
            completedUtc);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The gift processing attempt is no longer active.");
        }

        return ReadRecord(reader);
    }

    public async Task<VirtuousGiftProcessingRecord> FailAsync(
        long giftId,
        Guid attemptId,
        string failureMessage,
        CancellationToken cancellationToken = default)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId),
                "Gift ID must be greater than zero.");
        }

        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Attempt ID cannot be empty.",
                nameof(attemptId));
        }

        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            throw new ArgumentException(
                "Failure message cannot be empty.",
                nameof(failureMessage));
        }

        await EnsureDatabaseAsync(
            cancellationToken);

        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            UPDATE virtuous_gift_processing
            SET
                status = 'Failed',
                aplos_transaction_id = NULL,
                aplos_response = NULL,
                completed_utc = NULL,
                failure_message = @failureMessage
            WHERE
                gift_id = @giftId
                AND attempt_id = @attemptId
                AND status = 'Processing'
            RETURNING
                gift_id,
                attempt_id,
                status,
                aplos_transaction_id,
                aplos_response,
                attempt_count,
                created_utc,
                last_attempt_utc,
                completed_utc,
                failure_message;
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        command.Parameters.AddWithValue(
            "attemptId",
            attemptId);

        command.Parameters.AddWithValue(
            "failureMessage",
            failureMessage);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The gift processing attempt is no longer active.");
        }

        return ReadRecord(reader);
    }

    public async Task<VirtuousGiftProcessingRecord> RequireReconciliationAsync(
    long giftId,
    Guid attemptId,
    string failureMessage,
    CancellationToken cancellationToken = default)
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId),
                "Gift ID must be greater than zero.");
        }

        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Attempt ID cannot be empty.",
                nameof(attemptId));
        }

        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            throw new ArgumentException(
                "Failure message cannot be empty.",
                nameof(failureMessage));
        }

        await EnsureDatabaseAsync(
            cancellationToken);

        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            UPDATE virtuous_gift_processing
            SET
                status = 'RequiresReconciliation',
                aplos_transaction_id = NULL,
                aplos_response = NULL,
                completed_utc = NULL,
                failure_message = @failureMessage
            WHERE
                gift_id = @giftId
                AND attempt_id = @attemptId
                AND status = 'Processing'
            RETURNING
                gift_id,
                attempt_id,
                status,
                aplos_transaction_id,
                aplos_response,
                attempt_count,
                created_utc,
                last_attempt_utc,
                completed_utc,
                failure_message;
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        command.Parameters.AddWithValue(
            "attemptId",
            attemptId);

        command.Parameters.AddWithValue(
            "failureMessage",
            failureMessage);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The gift processing attempt is no longer active.");
        }

        return ReadRecord(reader);
    }

    private async Task EnsureDatabaseAsync(
        CancellationToken cancellationToken)
    {
        await using var connection =
            new NpgsqlConnection(
                _connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS virtuous_gift_processing
            (
                gift_id BIGINT NOT NULL PRIMARY KEY,
                attempt_id UUID NOT NULL,
                status TEXT NOT NULL,
                aplos_transaction_id BIGINT NULL,
                aplos_response TEXT NULL,
                attempt_count INTEGER NOT NULL,
                created_utc TIMESTAMPTZ NOT NULL,
                last_attempt_utc TIMESTAMPTZ NULL,
                completed_utc TIMESTAMPTZ NULL,
                failure_message TEXT NULL,

                CONSTRAINT ck_virtuous_gift_processing_status
                    CHECK (status IN (
                        'Processing',
                        'Completed',
                        'Failed',
                        'RequiresReconciliation'
                    )),

                CONSTRAINT ck_virtuous_gift_processing_attempt_count
                    CHECK (attempt_count >= 1),

                CONSTRAINT ck_virtuous_gift_processing_aplos_transaction_id
                    CHECK (
                        aplos_transaction_id IS NULL
                        OR aplos_transaction_id > 0
                    )
            );
            """;

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private static async Task AcquireGiftLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long giftId,
        CancellationToken cancellationToken)
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

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private static async Task<VirtuousGiftProcessingRecord?> GetRecordAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long giftId,
        CancellationToken cancellationToken)
    {
        await using var command =
            connection.CreateCommand();

        command.Transaction =
            transaction;

        command.CommandText =
            """
            SELECT
                gift_id,
                attempt_id,
                status,
                aplos_transaction_id,
                aplos_response,
                attempt_count,
                created_utc,
                last_attempt_utc,
                completed_utc,
                failure_message
            FROM virtuous_gift_processing
            WHERE gift_id = @giftId;
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(
                cancellationToken))
        {
            return null;
        }

        return ReadRecord(reader);
    }

    private static async Task<VirtuousGiftProcessingClaim>
        CreateProcessingRecordAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long giftId,
            CancellationToken cancellationToken)
    {
        var attemptId =
            Guid.NewGuid();

        var now =
            DateTime.UtcNow;

        await using var command =
            connection.CreateCommand();

        command.Transaction =
            transaction;

        command.CommandText =
            """
            INSERT INTO virtuous_gift_processing
            (
                gift_id,
                attempt_id,
                status,
                attempt_count,
                created_utc,
                last_attempt_utc
            )
            VALUES
            (
                @giftId,
                @attemptId,
                'Processing',
                1,
                @createdUtc,
                @lastAttemptUtc
            );
            """;

        command.Parameters.AddWithValue(
            "giftId",
            giftId);

        command.Parameters.AddWithValue(
            "attemptId",
            attemptId);

        command.Parameters.AddWithValue(
            "createdUtc",
            now);

        command.Parameters.AddWithValue(
            "lastAttemptUtc",
            now);

        await command.ExecuteNonQueryAsync(
            cancellationToken);

        return new VirtuousGiftProcessingClaim
        {
            Record =
                new VirtuousGiftProcessingRecord
                {
                    GiftId = giftId,
                    AttemptId = attemptId,
                    Status =
                        VirtuousGiftProcessingStatus.Processing,
                    AttemptCount = 1,
                    CreatedUtc = now,
                    LastAttemptUtc = now
                },
            ShouldProcess = true
        };
    }

       private static VirtuousGiftProcessingRecord ReadRecord(
        NpgsqlDataReader reader)
    {
        return new VirtuousGiftProcessingRecord
        {
            GiftId =
                reader.GetInt64(0),

            AttemptId =
                reader.GetGuid(1),

            Status =
                Enum.Parse<VirtuousGiftProcessingStatus>(
                    reader.GetString(2)),

            AplosTransactionId =
                reader.IsDBNull(3)
                    ? null
                    : reader.GetInt64(3),

            AplosResponse =
                reader.IsDBNull(4)
                    ? null
                    : reader.GetString(4),

            AttemptCount =
                reader.GetInt32(5),

            CreatedUtc =
                reader.GetDateTime(6),

            LastAttemptUtc =
                reader.IsDBNull(7)
                    ? null
                    : reader.GetDateTime(7),

            CompletedUtc =
                reader.IsDBNull(8)
                    ? null
                    : reader.GetDateTime(8),

            FailureMessage =
                reader.IsDBNull(9)
                    ? null
                    : reader.GetString(9)
        };
    }
}
