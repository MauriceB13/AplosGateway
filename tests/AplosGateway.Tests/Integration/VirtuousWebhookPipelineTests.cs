using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AplosGateway.Core.Virtuous;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;


namespace AplosGateway.Tests.Integration;

public sealed class VirtuousWebhookPipelineTests
{
    [Fact]
public async Task ProcessGift_Success_ReturnsStablePublicResponse()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });

                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<IVirtuousGiftService>();

                            services.AddSingleton<IVirtuousGiftService>(
                                new SuccessfulGiftService());
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            "local-dev-key-12345");

    var request =
        new VirtuousGiftWebhookRequest
        {
            Event = "GiftCreate",
            Gift =
                new VirtuousWebhookGift
                {
                    Id = 38241,
                    ContactName = "John Smith",
                    GiftDateUtc =
                        new DateTime(
                            2026,
                            9,
                            2,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),
                    Amount = 150m,
                    CurrencyCode = "USD"
                },
            Organization =
                new VirtuousOrganization
                {
                    Id = 1001,
                    Name = "Maine Central Institute"
                }
        };

    var response =
        await client.PostAsJsonAsync(
            "/api/virtuous/gift",
            request);

    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    using var document =
        JsonDocument.Parse(json);

    var root =
        document.RootElement;

    Assert.Equal(
        "processed",
        root.GetProperty("status").GetString());

    Assert.Equal(
        38241,
        root.GetProperty("giftId").GetInt64());

    Assert.Equal(
        70064235,
        root.GetProperty("aplosTransactionId").GetInt64());
}

[Fact]
public async Task RetryFailedGift_Success_ReturnsStablePublicResponse()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });

                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<IVirtuousGiftService>();

                            services.AddSingleton<IVirtuousGiftService>(
                                new SuccessfulGiftService());
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            "local-dev-key-12345");

    var response =
        await client.PostAsJsonAsync(
            "/api/virtuous/gift/retry",
            CreateWebhookRequest());

    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    using var document =
        JsonDocument.Parse(json);

    var root =
        document.RootElement;

    Assert.Equal(
        "processed",
        root.GetProperty("status").GetString());

    Assert.Equal(
        38241,
        root.GetProperty("giftId").GetInt64());

    Assert.Equal(
        70064235,
        root.GetProperty("aplosTransactionId").GetInt64());
}

    [Fact]
    public async Task ProcessGift_OperationalFailure_ReturnsInternalServerError()
    {
        await using var factory =
            new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });

                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IVirtuousGiftService>();

                        services.AddScoped<IVirtuousGiftService>(
                            _ => new ThrowingGiftService());
                    });
                });

        using var client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                "local-dev-key-12345");

        var request =
            CreateWebhookRequest();

        var response =
            await client.PostAsJsonAsync(
                "/api/virtuous/gift",
                request);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

            var json =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        Assert.Equal(
            "An unexpected error occurred.",
            root.GetProperty("error").GetString());

        Assert.True(
            root.TryGetProperty(
                "traceId",
                out var traceId));

        Assert.False(
            string.IsNullOrWhiteSpace(
                traceId.GetString()));

        Assert.DoesNotContain(
            "Sensitive downstream detail",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "secret-token-12345",
            json,
            StringComparison.OrdinalIgnoreCase);
    }

[Fact]
public async Task ProcessGift_MissingAuthorizationHeader_ReturnsUnauthorized()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    var response =
        await client.PostAsJsonAsync(
            "/api/virtuous/gift",
            CreateWebhookRequest());

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    Assert.Contains(
        "Missing authorization header.",
        json,
        StringComparison.Ordinal);
}

[Fact]
public async Task ProcessGift_InvalidApiKey_ReturnsUnauthorized()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            "wrong-api-key");

    var response =
        await client.PostAsJsonAsync(
            "/api/virtuous/gift",
            CreateWebhookRequest());

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    Assert.Contains(
        "Invalid API key.",
        json,
        StringComparison.Ordinal);
}

[Fact]
public async Task RetryFailedGift_RequiresReconciliation_ReturnsConflict()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });

                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<IVirtuousGiftService>();

                            services.AddSingleton<IVirtuousGiftService>(
                                new ReconciliationGiftService());
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            "local-dev-key-12345");

    var response =
        await client.PostAsJsonAsync(
            "/api/virtuous/gift/retry",
            CreateWebhookRequest());

    Assert.Equal(
        HttpStatusCode.Conflict,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    using var document =
        JsonDocument.Parse(json);

    var root =
        document.RootElement;

    Assert.Equal(
        38241,
        root.GetProperty("giftId").GetInt64());

    Assert.Equal(
        "RequiresReconciliation",
        root.GetProperty("processingStatus").GetString());

    Assert.True(
        root.TryGetProperty(
            "traceId",
            out var traceId));

    Assert.False(
        string.IsNullOrWhiteSpace(
            traceId.GetString()));
}

[Fact]
public async Task RetryFailedGift_FingerprintMismatch_ReturnsConflict()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });

                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<IVirtuousGiftService>();

                            services.AddSingleton<IVirtuousGiftService>(
                                new FingerprintMismatchGiftService());
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            "local-dev-key-12345");

    var response =
        await client.PostAsJsonAsync(
            "/api/virtuous/gift/retry",
            CreateWebhookRequest());

    Assert.Equal(
        HttpStatusCode.Conflict,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    using var document =
        JsonDocument.Parse(json);

    var root =
        document.RootElement;

    Assert.Equal(
        38241,
        root.GetProperty("giftId").GetInt64());

    Assert.Equal(
        "Virtuous gift 38241 does not match the originally received gift.",
        root.GetProperty("error").GetString());

    Assert.False(
        root.TryGetProperty(
            "processingStatus",
            out _));

    Assert.True(
        root.TryGetProperty(
            "traceId",
            out var traceId));

    Assert.False(
        string.IsNullOrWhiteSpace(
            traceId.GetString()));

    Assert.DoesNotContain(
        "fingerprint",
        json,
        StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task Health_DoesNotRequireAuthorization()
{
    using var factory =
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            ConfigureTestSettings(configuration);
                        });
                });

    using var client =
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

    var response =
        await client.GetAsync(
            "/health");

    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);
}

    private static void ConfigureTestSettings(
        IConfigurationBuilder configuration)
    {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Security:ApiKey"] =
                        "local-dev-key-12345",

                    ["Virtuous:OrganizationId"] =
                        "1001",

                    ["TransactionMapping:DepositAccountNumber"] =
                        "2001",

                    ["TransactionMapping:IncomeAccountNumber"] =
                        "4001",

                    ["TransactionMapping:FundId"] =
                        "5001"
                });
    }

    private static VirtuousGiftWebhookRequest
        CreateWebhookRequest()
    {
        return new VirtuousGiftWebhookRequest
        {
            Event = "GiftCreate",

            EventId =
                "test-event-id",

            EventDateTimeUtc =
                DateTime.UtcNow,

            Organization =
                new VirtuousOrganization
                {
                    Id = 1001,
                    Name = "Maine Central Institute"
                },

            Gift =
                new VirtuousWebhookGift
                {
                    Id = 38241,
                    ContactName = "John Smith",

                    GiftDateUtc =
                        new DateTime(
                            2026,
                            9,
                            2,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),

                    Amount = 150m,
                    CurrencyCode = "USD",

                    GiftDesignations =
                    [
                        new VirtuousGiftDesignation
                        {
                            Id = 38414,
                            GiftId = 38241,
                            ProjectId = 104,
                            Project = "Test Project",
                            ProjectCode = "49999-25",
                            AmountDesignated = 150m
                        }
                    ]
                }
        };
    }

    private sealed class ThrowingGiftService
        : IVirtuousGiftService
    {
       public Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
            VirtuousGift gift,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Sensitive downstream detail: secret-token-12345");
        }

        public Task<VirtuousGiftProcessingResult> RetryFailedGiftAsync(
            VirtuousGift gift,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Sensitive downstream detail: secret-token-12345");
        }
    }

    private sealed class ReconciliationGiftService
    : IVirtuousGiftService
{
    public Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "This test service supports retry only.");
    }

    public Task<VirtuousGiftProcessingResult> RetryFailedGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        throw new VirtuousGiftProcessingStateException(
            gift.Id,
            VirtuousGiftProcessingStatus.RequiresReconciliation);
    }
}
    
    private sealed class FingerprintMismatchGiftService
    : IVirtuousGiftService
{
    public Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "This test service supports retry only.");
    }

    public Task<VirtuousGiftProcessingResult> RetryFailedGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        throw new VirtuousGiftFingerprintMismatchException(
            gift.Id);
    }
}

    private sealed class SuccessfulGiftService
    : IVirtuousGiftService
{
    public Task<VirtuousGiftProcessingResult> ProcessGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new VirtuousGiftProcessingResult
            {
                Status = "processed",
                GiftId = gift.Id,
                AplosTransactionId = 70064235
            });
    }

    public Task<VirtuousGiftProcessingResult> RetryFailedGiftAsync(
        VirtuousGift gift,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new VirtuousGiftProcessingResult
            {
                Status = "processed",
                GiftId = gift.Id,
                AplosTransactionId = 70064235
            });
    }
}
}