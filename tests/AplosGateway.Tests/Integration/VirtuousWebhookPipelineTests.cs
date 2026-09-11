using System.Net;
using System.Net.Http.Json;
using AplosGateway.Core.Virtuous;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AplosGateway.Tests.Integration;

public sealed class VirtuousWebhookPipelineTests
{
    [Fact]
    public async Task ProcessGift_OperationalFailure_ReturnsInternalServerError()
    {
        await using var factory =
            new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

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
                    Id = 7472,
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
        public Task<string> ProcessGiftAsync(
            VirtuousGift gift,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Aplos unavailable.");
        }
    }
}