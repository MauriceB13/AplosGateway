using AplosGateway.Api.Configuration;
using AplosGateway.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AplosGateway.Tests.Configuration;

public sealed class SecurityOptionsTests
{
    [Fact]
    public void AddGatewayServices_MissingApiKey_FailsValidation()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Security:ApiKey"] = ""
                    })
                .Build();

        var services =
            new ServiceCollection();

        services.AddGatewayServices(
            configuration);

        using var provider =
            services.BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                IOptions<SecurityOptions>>();

        var exception =
            Assert.Throws<OptionsValidationException>(
                () => _ = options.Value);

        Assert.Contains(
            "Security:ApiKey",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}