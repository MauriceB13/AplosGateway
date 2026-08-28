using System.Text.Json;
using AplosGateway.Core.Aplos;
using AplosGateway.Core.Configuration;
using AplosGateway.Core.Transactions;
using AplosGateway.Infrastructure.Transactions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AplosGateway.Tests.Transactions;

public sealed class AplosTransactionServiceTests
{
    [Fact]
    public async Task CreateTransactionAsync_SerializesAndPostsTransaction()
    {
        var apiClient = new StubAplosApiClient();

        var options = Options.Create(
            new AplosOptions
            {
                AllowTransactionPosting = true
            });

        var service =
            new AplosTransactionService(
                apiClient,
                options);

        var request =
            new AplosTransactionRequest
            {
                Note = "Virtuous Gift 12345",
                Date = "2026-08-28",
                Contact = new AplosTransactionContact
                {
                    CompanyName = "Virtuous",
                    Type = "company"
                },
                Lines =
                [
                    new AplosTransactionLine
                    {
                        Amount = 100.00m,
                        Account = new AplosTransactionAccount
                        {
                            AccountNumber = 20114
                        },
                        Fund = new AplosFund
                        {
                            Id = 100
                        }
                    },
                    new AplosTransactionLine
                    {
                        Amount = -100.00m,
                        Account = new AplosTransactionAccount
                        {
                            AccountNumber = 41025
                        },
                        Fund = new AplosFund
                        {
                            Id = 100
                        }
                    }
                ]
            };

        var result =
            await service.CreateTransactionAsync(request);

        Assert.Equal(
            """{"status":"created"}""",
            result);

        Assert.Equal(
            1,
            apiClient.PostCallCount);

        Assert.Equal(
            "transactions",
            apiClient.LastRelativePath);

        Assert.NotNull(
            apiClient.LastJsonContent);

        using var document =
            JsonDocument.Parse(
                apiClient.LastJsonContent!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Virtuous Gift 12345",
            root.GetProperty("note").GetString());

        Assert.Equal(
            "2026-08-28",
            root.GetProperty("date").GetString());

        var lines =
            root.GetProperty("lines");

        Assert.Equal(
            2,
            lines.GetArrayLength());

        Assert.Equal(
            100.00m,
            lines[0]
                .GetProperty("amount")
                .GetDecimal());

        Assert.Equal(
            20114,
            lines[0]
                .GetProperty("account")
                .GetProperty("account_number")
                .GetInt32());

        Assert.Equal(
            -100.00m,
            lines[1]
                .GetProperty("amount")
                .GetDecimal());

        Assert.Equal(
            41025,
            lines[1]
                .GetProperty("account")
                .GetProperty("account_number")
                .GetInt32());
    }

    [Fact]
    public async Task CreateTransactionAsync_ThrowsWhenRequestIsNull()
    {
        var apiClient =
            new StubAplosApiClient();

        var options = Options.Create(
            new AplosOptions
            {
                AllowTransactionPosting = true
            });

        var service =
            new AplosTransactionService(
                apiClient,
                options);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateTransactionAsync(null!));

        Assert.Equal(
            0,
            apiClient.PostCallCount);
    }

    [Fact]
    public async Task CreateTransactionAsync_DoesNotPostWhenPostingIsDisabled()
    {
        var apiClient =
            new StubAplosApiClient();

        var options = Options.Create(
            new AplosOptions
            {
                AllowTransactionPosting = false
            });

        var service =
            new AplosTransactionService(
                apiClient,
                options);

        var request =
            new AplosTransactionRequest
            {
                Note = "Should not post"
            };

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateTransactionAsync(request));

        Assert.Equal(
            "Aplos transaction posting is disabled.",
            exception.Message);

        Assert.Equal(
            0,
            apiClient.PostCallCount);
    }

    private sealed class StubAplosApiClient
        : IAplosApiClient
    {
        public int PostCallCount { get; private set; }

        public string? LastRelativePath { get; private set; }

        public string? LastJsonContent { get; private set; }

        public Task<string> GetAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<string> PostAsync(
            string relativePath,
            string jsonContent,
            CancellationToken cancellationToken = default)
        {
            PostCallCount++;

            LastRelativePath =
                relativePath;

            LastJsonContent =
                jsonContent;

            return Task.FromResult(
                """{"status":"created"}""");
        }
    }
}