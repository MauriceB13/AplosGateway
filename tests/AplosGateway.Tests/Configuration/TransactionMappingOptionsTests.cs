using AplosGateway.Api.Extensions;
using AplosGateway.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AplosGateway.Tests.Configuration;

public sealed class TransactionMappingOptionsTests
{
    [Fact]
    public void AddGatewayServices_ValidTransactionMapping_BindsOptions()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["TransactionMapping:DepositAccountNumber"] = "20114",
                        ["TransactionMapping:IncomeAccountNumber"] = "41025",
                        ["TransactionMapping:FundId"] = "492387"
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
                    IOptions<TransactionMappingOptions>>()
                .Value;

        Assert.Equal(
            20114,
            options.DepositAccountNumber);

        Assert.Equal(
            41025,
            options.IncomeAccountNumber);

        Assert.Equal(
            492387,
            options.FundId);
    }

    [Fact]
    public void AddGatewayServices_MissingTransactionMapping_FailsValidation()
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
                IOptions<TransactionMappingOptions>>();

        var exception =
            Assert.Throws<OptionsValidationException>(
                () => _ = options.Value);

        Assert.Contains(
            "TransactionMapping",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}