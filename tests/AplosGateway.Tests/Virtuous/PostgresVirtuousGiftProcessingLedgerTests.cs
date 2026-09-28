using AplosGateway.Core.Configuration;
using AplosGateway.Core.Virtuous;
using AplosGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AplosGateway.Tests.Virtuous;

[Collection("PostgreSQL Integration")]
public sealed class PostgresVirtuousGiftProcessingLedgerTests
{
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
                12345);

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
                12345);

        var secondLedger =
            CreateLedger();

        var secondClaim =
            await secondLedger.BeginProcessingAsync(
                12345);

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
                12345);

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
                12345);

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
                12345);

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
                12345);

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
            12345);

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
            12345);

    var failed =
        await firstLedger.FailAsync(
            12345,
            firstClaim.Record.AttemptId,
            "Definite test failure.");

    var secondLedger =
        CreateLedger();

    var duplicateClaim =
        await secondLedger.BeginProcessingAsync(
            12345);

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
public async Task RequireReconciliationAsync_ActiveAttempt_MarksGiftForReconciliation()
{
    await ClearDatabaseAsync();

    var ledger =
        CreateLedger();

    var claim =
        await ledger.BeginProcessingAsync(
            12345);

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
            12345);

    await firstLedger.RequireReconciliationAsync(
        12345,
        firstClaim.Record.AttemptId,
        "Aplos transaction outcome is uncertain.");

    var secondLedger =
        CreateLedger();

    var duplicateClaim =
        await secondLedger.BeginProcessingAsync(
            12345);

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
}
