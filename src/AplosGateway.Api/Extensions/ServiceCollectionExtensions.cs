using AplosGateway.Api.Configuration;
using AplosGateway.Core.Security;
using AplosGateway.Infrastructure.Security;
using AplosGateway.Core.Authentication;
using AplosGateway.Infrastructure.Authentication;
using AplosGateway.Core.Configuration;
using AplosGateway.Core.Aplos;
using AplosGateway.Infrastructure.Aplos;
using AplosGateway.Core.Transactions;
using AplosGateway.Infrastructure.Transactions;

namespace AplosGateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.Configure<GatewayOptions>(
            configuration.GetSection(GatewayOptions.SectionName));

        services.Configure<SecurityOptions>(
            configuration.GetSection(SecurityOptions.SectionName));

        services.Configure<AplosOptions>(
            configuration.GetSection(AplosOptions.SectionName));

        services.AddMemoryCache();

        services.AddSingleton<IAplosTokenDecryptor, RsaAplosTokenDecryptor>();

        services.AddHttpClient<
    IAplosAuthenticationService,
    AplosAuthenticationService>();

    services.AddHttpClient<
    IAplosApiClient,
    AplosApiClient>();

    services.AddScoped<
    IAplosTransactionService,
    AplosTransactionService>();

        return services;
    }
}