using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AplosGateway.Tests.Integration;

public sealed class HealthPipelineTests
{
    [Fact]
    public async Task Ready_ValidConfiguration_ReturnsOk()
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
                                configuration.AddInMemoryCollection(
                                    new Dictionary<string, string?>
                                    {
                                        ["Security:ApiKey"] =
                                            "test-api-key",

                                        ["Aplos:BaseUrl"] =
                                            "https://app.aplos.com/hermes/api/v1",

                                        ["Aplos:ClientId"] =
                                            "test-client-id",

                                        ["Aplos:PrivateKey"] =
                                            "test-private-key",

                                        ["Virtuous:OrganizationId"] =
                                            "7472",

                                        ["TransactionMapping:DepositAccountNumber"] =
                                            "20114",

                                        ["TransactionMapping:IncomeAccountNumber"] =
                                            "41025",

                                        ["TransactionMapping:FundId"] =
                                            "492387",

                                        ["Idempotency:ConnectionString"] =
                                            "Data Source=Data/test.db"
                                    });
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
                "/health/ready");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            "Ready",
            document.RootElement
                .GetProperty("status")
                .GetString());
    }

    [Fact]
public async Task Ready_MissingAplosPrivateKey_ReturnsServiceUnavailable()
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
                            configuration.AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    ["Security:ApiKey"] =
                                        "test-api-key",

                                    ["Aplos:BaseUrl"] =
                                        "https://app.aplos.com/hermes/api/v1",

                                    ["Aplos:ClientId"] =
                                        "test-client-id",

                                    ["Aplos:PrivateKey"] =
                                        "",

                                    ["Virtuous:OrganizationId"] =
                                        "7472",

                                    ["TransactionMapping:DepositAccountNumber"] =
                                        "20114",

                                    ["TransactionMapping:IncomeAccountNumber"] =
                                        "41025",

                                    ["TransactionMapping:FundId"] =
                                        "492387",

                                    ["Idempotency:ConnectionString"] =
                                        "Data Source=Data/test.db"
                                });
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
            "/health/ready");

    Assert.Equal(
        HttpStatusCode.ServiceUnavailable,
        response.StatusCode);

    var json =
        await response.Content.ReadAsStringAsync();

    using var document =
        JsonDocument.Parse(json);

    Assert.Equal(
        "NotReady",
        document.RootElement
            .GetProperty("status")
            .GetString());

    Assert.DoesNotContain(
        "PrivateKey",
        json,
        StringComparison.OrdinalIgnoreCase);

    Assert.DoesNotContain(
        "test-client-id",
        json,
        StringComparison.OrdinalIgnoreCase);
}
}