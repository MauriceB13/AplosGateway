using VirtuousGateway.Core.Configuration;
using VirtuousGateway.Core.Virtuous;
using VirtuousGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;
using Npgsql;

namespace VirtuousGateway.Tests.Virtuous;

[Collection("PostgreSQL Integration")]
public sealed class PostgresVirtuousGiftProcessingLedgerTests
{

    private const string GiftFingerprint =
    "TEST-GIFT-FINGERPRINT";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable(
            "ProcessingLedger__ConnectionString")
        ?? throw new InvalidOperationException(
            "ProcessingLedger__ConnectionString must be configured to run PostgreSQL integration tests.");

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task BeginProcessingAsync_NewGift_ClaimsProcessing()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var claim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.True(
            claim.ShouldProcess);

        Assert.Equal(
            12345,
            claim.Record.GiftId);

        Assert.NotEqual(
            Guid.Empty,
            claim.Record.AttemptId);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            claim.Record.Status);

        Assert.Equal(
            1,
            claim.Record.AttemptCount);

        Assert.NotEqual(
            default,
            claim.Record.CreatedUtc);

        Assert.NotNull(
            claim.Record.LastAttemptUtc);
    }

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task BeginProcessingAsync_ExistingProcessingGift_DoesNotClaimAgain()
    {
        await ClearDatabaseAsync();

        var firstLedger =
            CreateLedger();

        var firstClaim =
            await firstLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        var secondLedger =
            CreateLedger();

        var secondClaim =
            await secondLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.True(
            firstClaim.ShouldProcess);

        Assert.False(
            secondClaim.ShouldProcess);

        Assert.Equal(
            firstClaim.Record.AttemptId,
            secondClaim.Record.AttemptId);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            secondClaim.Record.Status);

        Assert.Equal(
            1,
            secondClaim.Record.AttemptCount);

        Assert.Equal(
            GiftFingerprint,
            secondClaim.Record.GiftFingerprint);
    }

            [Trait("Category", "PostgreSqlIntegration")]
            [Fact]
            public async Task BeginProcessingAsync_ExistingGiftWithDifferentFingerprint_IsRejected()
            {
                await ClearDatabaseAsync();

                var ledger =
                    CreateLedger();

                const long giftId = 12345;

                var firstClaim =
                    await ledger.BeginProcessingAsync(
                        giftId,
                        GiftFingerprint);

                var exception =
                    await Assert.ThrowsAsync<VirtuousGiftFingerprintMismatchException>(
                        () =>
                            ledger.BeginProcessingAsync(
                                giftId,
                                "DIFFERENT-FINGERPRINT"));

                Assert.Equal(
                    giftId,
                    exception.GiftId);

                var existingClaim =
                    await ledger.BeginProcessingAsync(
                        giftId,
                        GiftFingerprint);

                Assert.False(
                    existingClaim.ShouldProcess);

                Assert.Equal(
                    VirtuousGiftProcessingStatus.Processing,
                    existingClaim.Record.Status);

                Assert.Equal(
                    firstClaim.Record.AttemptId,
                    existingClaim.Record.AttemptId);

                Assert.Equal(
                    firstClaim.Record.AttemptCount,
                    existingClaim.Record.AttemptCount);

                Assert.Equal(
                    GiftFingerprint,
                    existingClaim.Record.GiftFingerprint);
            }

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task CompleteAsync_ActiveAttempt_CompletesGift()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var claim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        const string aplosResponse =
            """{"status":200,"transactionId":70064235}""";

        var completed =
            await ledger.CompleteAsync(
                12345,
                claim.Record.AttemptId,
                70064235,
                aplosResponse);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Completed,
            completed.Status);

        Assert.Equal(
            70064235,
            completed.AplosTransactionId);

        Assert.Equal(
            aplosResponse,
            completed.AplosResponse);

        Assert.NotNull(
            completed.CompletedUtc);

        Assert.Equal(
            1,
            completed.AttemptCount);

        var duplicateClaim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.False(
            duplicateClaim.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Completed,
            duplicateClaim.Record.Status);

        Assert.Equal(
            70064235,
            duplicateClaim.Record.AplosTransactionId);
    }

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task CompleteAsync_WrongAttemptId_IsRejected()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var claim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.NotEqual(
            Guid.Empty,
            claim.Record.AttemptId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    ledger.CompleteAsync(
                        12345,
                        Guid.NewGuid(),
                        70064235,
                        """{"status":200,"transactionId":70064235}"""));

        Assert.Equal(
            "The gift processing attempt is no longer active.",
            exception.Message);

        var existing =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.False(
            existing.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            existing.Record.Status);

        Assert.Equal(
            claim.Record.AttemptId,
            existing.Record.AttemptId);
    }

    [Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task FailAsync_ActiveAttempt_MarksGiftFailed()
{
    await ClearDatabaseAsync();

    var ledger =
        CreateLedger();

    var claim =
        await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    var failed =
        await ledger.FailAsync(
            12345,
            claim.Record.AttemptId,
            "Definite test failure.");

    Assert.Equal(
        VirtuousGiftProcessingStatus.Failed,
        failed.Status);

    Assert.Equal(
        "Definite test failure.",
        failed.FailureMessage);

    Assert.Null(
        failed.AplosTransactionId);

    Assert.Null(
        failed.AplosResponse);

    Assert.Null(
        failed.CompletedUtc);

    Assert.Equal(
        1,
        failed.AttemptCount);
}

[Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task BeginProcessingAsync_FailedGift_DoesNotAutomaticallyRetry()
{
    await ClearDatabaseAsync();

    var firstLedger =
        CreateLedger();

    var firstClaim =
        await firstLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    var failed =
        await firstLedger.FailAsync(
            12345,
            firstClaim.Record.AttemptId,
            "Definite test failure.");

    var secondLedger =
        CreateLedger();

    var duplicateClaim =
        await secondLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    Assert.False(
        duplicateClaim.ShouldProcess);

    Assert.Equal(
        VirtuousGiftProcessingStatus.Failed,
        duplicateClaim.Record.Status);

    Assert.Equal(
        firstClaim.Record.AttemptId,
        duplicateClaim.Record.AttemptId);

    Assert.Equal(
        1,
        duplicateClaim.Record.AttemptCount);

    Assert.Equal(
        "Definite test failure.",
        duplicateClaim.Record.FailureMessage);

    Assert.Null(
        duplicateClaim.Record.AplosTransactionId);

    Assert.Null(
        duplicateClaim.Record.AplosResponse);

    Assert.Null(
        duplicateClaim.Record.CompletedUtc);

    Assert.Equal(
        failed.Status,
        duplicateClaim.Record.Status);
}

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task RetryFailedAsync_FailedGift_ClaimsNewAttempt()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var firstClaim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        var failed =
            await ledger.FailAsync(
                12345,
                firstClaim.Record.AttemptId,
                "Definite test failure.");

        var retryClaim =
            await ledger.RetryFailedAsync(
                12345,
                GiftFingerprint);

        Assert.True(
            retryClaim.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            retryClaim.Record.Status);

        Assert.NotEqual(
            failed.AttemptId,
            retryClaim.Record.AttemptId);

        Assert.NotEqual(
            Guid.Empty,
            retryClaim.Record.AttemptId);

        Assert.Equal(
            2,
            retryClaim.Record.AttemptCount);

        Assert.Equal(
            failed.CreatedUtc,
            retryClaim.Record.CreatedUtc);

        Assert.NotNull(
            retryClaim.Record.LastAttemptUtc);

        Assert.True(
            retryClaim.Record.LastAttemptUtc >=
            failed.LastAttemptUtc);

        Assert.Null(
            retryClaim.Record.FailureMessage);

        Assert.Null(
            retryClaim.Record.AplosTransactionId);

        Assert.Null(
            retryClaim.Record.AplosResponse);

        Assert.Null(
            retryClaim.Record.CompletedUtc);
    }

    [Fact]
    public async Task RetryFailedAsync_DifferentFingerprint_IsRejectedWithoutChangingRecord()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        const long giftId = 12345;

        var firstClaim =
            await ledger.BeginProcessingAsync(
                giftId,
                GiftFingerprint);

        var failed =
            await ledger.FailAsync(
                giftId,
                firstClaim.Record.AttemptId,
                "Definite test failure.");

        var exception =
            await Assert.ThrowsAsync<VirtuousGiftFingerprintMismatchException>(
                () =>
                    ledger.RetryFailedAsync(
                        giftId,
                        "DIFFERENT-FINGERPRINT"));

        Assert.Equal(
            giftId,
            exception.GiftId);

        var existingClaim =
            await ledger.BeginProcessingAsync(
                giftId,
                GiftFingerprint);

        Assert.False(
            existingClaim.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Failed,
            existingClaim.Record.Status);

        Assert.Equal(
            failed.AttemptId,
            existingClaim.Record.AttemptId);

        Assert.Equal(
            failed.AttemptCount,
            existingClaim.Record.AttemptCount);

        Assert.Equal(
            GiftFingerprint,
            existingClaim.Record.GiftFingerprint);

        Assert.Equal(
            failed.FailureMessage,
            existingClaim.Record.FailureMessage);
    }

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task RetryFailedAsync_ReconciliationGift_IsRejected()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var firstClaim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        var reconciliation =
            await ledger.RequireReconciliationAsync(
                12345,
                firstClaim.Record.AttemptId,
                "Aplos transaction outcome is uncertain.");

        var exception =
            await Assert.ThrowsAsync<VirtuousGiftProcessingStateException>(
                () =>
                    ledger.RetryFailedAsync(
                12345,
                GiftFingerprint));

        Assert.Equal(
            12345,
            exception.GiftId);

        Assert.Equal(
            VirtuousGiftProcessingStatus.RequiresReconciliation,
            exception.Status);

        var existing =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.False(
            existing.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.RequiresReconciliation,
            existing.Record.Status);

        Assert.Equal(
            reconciliation.AttemptId,
            existing.Record.AttemptId);

        Assert.Equal(
            1,
            existing.Record.AttemptCount);

        Assert.Equal(
            "Aplos transaction outcome is uncertain.",
            existing.Record.FailureMessage);
    }

    [Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task RetryFailedAsync_ProcessingGift_IsRejected()
{
    await ClearDatabaseAsync();

    var ledger =
        CreateLedger();

    var firstClaim =
        await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    var exception =
        await Assert.ThrowsAsync<VirtuousGiftProcessingStateException>(
            () =>
                ledger.RetryFailedAsync(
                12345,
                GiftFingerprint));

    Assert.Equal(
        12345,
        exception.GiftId);

    Assert.Equal(
        VirtuousGiftProcessingStatus.Processing,
        exception.Status);

    var existing =
        await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    Assert.False(
        existing.ShouldProcess);

    Assert.Equal(
        VirtuousGiftProcessingStatus.Processing,
        existing.Record.Status);

    Assert.Equal(
        firstClaim.Record.AttemptId,
        existing.Record.AttemptId);

    Assert.Equal(
        1,
        existing.Record.AttemptCount);
}

[Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task RetryFailedAsync_CompletedGift_IsRejected()
{
    await ClearDatabaseAsync();

    var ledger =
        CreateLedger();

    var firstClaim =
        await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    var completed =
        await ledger.CompleteAsync(
            12345,
            firstClaim.Record.AttemptId,
            70064235,
            """{"status":200,"transactionId":70064235}""");

    var exception =
        await Assert.ThrowsAsync<VirtuousGiftProcessingStateException>(
            () =>
                ledger.RetryFailedAsync(
                12345,
                GiftFingerprint));

    Assert.Equal(
        12345,
        exception.GiftId);

    Assert.Equal(
        VirtuousGiftProcessingStatus.Completed,
        exception.Status);

    var existing =
        await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    Assert.False(
        existing.ShouldProcess);

    Assert.Equal(
        VirtuousGiftProcessingStatus.Completed,
        existing.Record.Status);

    Assert.Equal(
        completed.AttemptId,
        existing.Record.AttemptId);

    Assert.Equal(
        1,
        existing.Record.AttemptCount);

    Assert.Equal(
        70064235,
        existing.Record.AplosTransactionId);
}

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task RetryFailedAsync_OldAttemptCannotCompleteRetriedGift()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var firstClaim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        await ledger.FailAsync(
            12345,
            firstClaim.Record.AttemptId,
            "Definite test failure.");

        var retryClaim =
            await ledger.RetryFailedAsync(
                12345,
                GiftFingerprint);

        Assert.NotEqual(
            firstClaim.Record.AttemptId,
            retryClaim.Record.AttemptId);

        var exception =
        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                ledger.CompleteAsync(
                    12345,
                    firstClaim.Record.AttemptId,
                    70064235,
                    """{"status":200,"transactionId":70064235}"""));

            Assert.Equal(
                "The gift processing attempt is no longer active.",
                exception.Message);

        var existing =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.False(
            existing.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            existing.Record.Status);

        Assert.Equal(
            retryClaim.Record.AttemptId,
            existing.Record.AttemptId);

        Assert.Equal(
            2,
            existing.Record.AttemptCount);

        Assert.Null(
            existing.Record.AplosTransactionId);

        Assert.Null(
            existing.Record.CompletedUtc);
    }

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task RetryFailedAsync_OldAttemptCannotFailRetriedGift()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var firstClaim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        await ledger.FailAsync(
            12345,
            firstClaim.Record.AttemptId,
            "Definite first-attempt failure.");

        var retryClaim =
            await ledger.RetryFailedAsync(
                12345,
                GiftFingerprint);

        Assert.NotEqual(
            firstClaim.Record.AttemptId,
            retryClaim.Record.AttemptId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    ledger.FailAsync(
                        12345,
                        firstClaim.Record.AttemptId,
                        "Late failure from the old attempt."));

        Assert.Equal(
            "The gift processing attempt is no longer active.",
            exception.Message);

        var existing =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.False(
            existing.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            existing.Record.Status);

        Assert.Equal(
            retryClaim.Record.AttemptId,
            existing.Record.AttemptId);

        Assert.Equal(
            2,
            existing.Record.AttemptCount);

        Assert.Null(
            existing.Record.FailureMessage);

        Assert.Null(
            existing.Record.AplosTransactionId);

        Assert.Null(
            existing.Record.CompletedUtc);
    }

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task RetryFailedAsync_ConcurrentRetries_OnlyOneClaimsNewAttempt()
    {
        await ClearDatabaseAsync();

        var setupLedger =
            CreateLedger();

        var firstClaim =
            await setupLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        await setupLedger.FailAsync(
            12345,
            firstClaim.Record.AttemptId,
            "Definite test failure.");

        var ledger1 =
            CreateLedger();

        var ledger2 =
            CreateLedger();

        var retryTask1 =
            ledger1.RetryFailedAsync(
                12345,
                GiftFingerprint);

        var retryTask2 =
            ledger2.RetryFailedAsync(
                12345,
                GiftFingerprint);

        var results =
            await Task.WhenAll(
                CaptureRetryAsync(retryTask1),
                CaptureRetryAsync(retryTask2));

        var successful =
            Assert.Single(
                results.Where(
                    result => result.Claim is not null));

        var rejected =
            Assert.Single(
                results.Where(
                    result => result.Exception is not null));

        Assert.True(
            successful.Claim!.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            successful.Claim.Record.Status);

        Assert.Equal(
            2,
            successful.Claim.Record.AttemptCount);

        var stateException =
            Assert.IsType<VirtuousGiftProcessingStateException>(
                rejected.Exception);

        Assert.Equal(
            12345,
            stateException.GiftId);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            stateException.Status);

        var existing =
            await setupLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        Assert.False(
            existing.ShouldProcess);

        Assert.Equal(
            VirtuousGiftProcessingStatus.Processing,
            existing.Record.Status);

        Assert.Equal(
            successful.Claim.Record.AttemptId,
            existing.Record.AttemptId);

        Assert.Equal(
            2,
            existing.Record.AttemptCount);
    }

    [Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task RetryFailedAsync_NonexistentGift_IsRejected()
{
    await ClearDatabaseAsync();

    var ledger =
        CreateLedger();

    var exception =
        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                ledger.RetryFailedAsync(
                12345,
                GiftFingerprint));

    Assert.Equal(
        "Virtuous gift 12345 does not have an existing processing record.",
        exception.Message);
}

    [Trait("Category", "PostgreSqlIntegration")]
    [Fact]
    public async Task RequireReconciliationAsync_ActiveAttempt_MarksGiftForReconciliation()
    {
        await ClearDatabaseAsync();

        var ledger =
            CreateLedger();

        var claim =
            await ledger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

        var reconciliation =
            await ledger.RequireReconciliationAsync(
                12345,
                claim.Record.AttemptId,
                "Aplos transaction outcome is uncertain.");

        Assert.Equal(
            VirtuousGiftProcessingStatus.RequiresReconciliation,
            reconciliation.Status);

        Assert.Equal(
            "Aplos transaction outcome is uncertain.",
            reconciliation.FailureMessage);

        Assert.Null(
            reconciliation.AplosTransactionId);

        Assert.Null(
            reconciliation.AplosResponse);

        Assert.Null(
            reconciliation.CompletedUtc);

        Assert.Equal(
            1,
            reconciliation.AttemptCount);
    }

[Trait("Category", "PostgreSqlIntegration")]
[Fact]
public async Task BeginProcessingAsync_ReconciliationGift_DoesNotClaimAgain()
{
    await ClearDatabaseAsync();

    var firstLedger =
        CreateLedger();

    var firstClaim =
        await firstLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    await firstLedger.RequireReconciliationAsync(
        12345,
        firstClaim.Record.AttemptId,
        "Aplos transaction outcome is uncertain.");

    var secondLedger =
        CreateLedger();

    var duplicateClaim =
        await secondLedger.BeginProcessingAsync(
                12345,
                GiftFingerprint);

    Assert.False(
        duplicateClaim.ShouldProcess);

    Assert.Equal(
        VirtuousGiftProcessingStatus.RequiresReconciliation,
        duplicateClaim.Record.Status);

    Assert.Equal(
        firstClaim.Record.AttemptId,
        duplicateClaim.Record.AttemptId);

    Assert.Equal(
        1,
        duplicateClaim.Record.AttemptCount);

    Assert.Equal(
        "Aplos transaction outcome is uncertain.",
        duplicateClaim.Record.FailureMessage);
}

        [Trait("Category", "PostgreSqlIntegration")]
        [Fact]
        public async Task RetryFailedAsync_LegacyRecordWithoutFingerprint_IsRejected()
        {
            await ClearDatabaseAsync();

            const long giftId = 12345;

            await using (var connection =
                new NpgsqlConnection(
                    ConnectionString))
            {
                await connection.OpenAsync();

                await using var command =
                    connection.CreateCommand();

                command.CommandText =
                    """
                    CREATE TABLE virtuous_gift_processing
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
                        failure_message TEXT NULL
                    );

                    INSERT INTO virtuous_gift_processing
                    (
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
                    )
                    VALUES
                    (
                        @giftId,
                        @attemptId,
                        'Failed',
                        NULL,
                        NULL,
                        1,
                        @createdUtc,
                        @lastAttemptUtc,
                        NULL,
                        'Legacy definite failure.'
                    );
                    """;

                command.Parameters.AddWithValue(
                    "giftId",
                    giftId);

                command.Parameters.AddWithValue(
                    "attemptId",
                    Guid.NewGuid());

                command.Parameters.AddWithValue(
                    "createdUtc",
                    DateTime.UtcNow);

                command.Parameters.AddWithValue(
                    "lastAttemptUtc",
                    DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();
            }

            var ledger =
                CreateLedger();

            var exception =
                await Assert.ThrowsAsync<VirtuousGiftFingerprintMismatchException>(
                    () =>
                        ledger.RetryFailedAsync(
                            giftId,
                            GiftFingerprint));

            Assert.Equal(
                giftId,
                exception.GiftId);

            await using var verificationConnection =
                new NpgsqlConnection(
                    ConnectionString);

            await verificationConnection.OpenAsync();

            await using var verificationCommand =
                verificationConnection.CreateCommand();

            verificationCommand.CommandText =
                """
                SELECT
                    gift_fingerprint,
                    status,
                    attempt_count,
                    failure_message
                FROM virtuous_gift_processing
                WHERE gift_id = @giftId;
                """;

            verificationCommand.Parameters.AddWithValue(
                "giftId",
                giftId);

            await using var reader =
                await verificationCommand.ExecuteReaderAsync();

            Assert.True(
                await reader.ReadAsync());

            Assert.True(
                reader.IsDBNull(0));

            Assert.Equal(
                "Failed",
                reader.GetString(1));

            Assert.Equal(
                1,
                reader.GetInt32(2));

            Assert.Equal(
                "Legacy definite failure.",
                reader.GetString(3));
        }

    private static PostgresVirtuousGiftProcessingLedger CreateLedger()
    {
        var options =
            Options.Create(
                new ProcessingLedgerOptions
                {
                    ConnectionString =
                        ConnectionString
                });

        return new PostgresVirtuousGiftProcessingLedger(
            options);
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

    private static async Task<RetryResult> CaptureRetryAsync(
    Task<VirtuousGiftProcessingClaim> retryTask)
    {
        try
        {
            return new RetryResult(
                await retryTask,
                null);
        }
        catch (Exception exception)
        {
            return new RetryResult(
                null,
                exception);
        }
    }

    private sealed record RetryResult(
        VirtuousGiftProcessingClaim? Claim,
        Exception? Exception);
}

