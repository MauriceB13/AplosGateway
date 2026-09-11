using AplosGateway.Api.Extensions;
using AplosGateway.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AplosGateway.Tests.Configuration;

public sealed class VirtuousOptionsTests
{
    [Fact]
    public void AddGatewayServices_ValidOrganizationId_BindsOptions()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Virtuous:OrganizationId"] = "7472"
                    })
                .Build();

        var services =
            new ServiceCollection();

        services.AddGatewayServices(
            configuration);

        using var provider =
            services.BuildServiceProvider();

        var options =
            provider
                .GetRequiredService<
                    IOptions<VirtuousOptions>>()
                .Value;

        Assert.Equal(
            7472,
            options.OrganizationId);
    }

    [Fact]
    public void AddGatewayServices_MissingOrganizationId_FailsValidation()
    {
        var configuration =
            new ConfigurationBuilder()
                .Build();

        var services =
            new ServiceCollection();

        services.AddGatewayServices(
            configuration);

        using var provider =
            services.BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                IOptions<VirtuousOptions>>();

        var exception =
            Assert.Throws<OptionsValidationException>(
                () => _ = options.Value);

        Assert.Contains(
            "Virtuous:OrganizationId",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}