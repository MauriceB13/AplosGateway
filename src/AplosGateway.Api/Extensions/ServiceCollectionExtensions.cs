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
using AplosGateway.Core.Virtuous;
using AplosGateway.Infrastructure.Virtuous;
using Microsoft.Extensions.Options;

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

        services.Configure<VirtuousOptions>(
            configuration.GetSection(VirtuousOptions.SectionName));    

        services.AddMemoryCache();

        services.AddSingleton<IAplosTokenDecryptor, RsaAplosTokenDecryptor>();

        services.AddSingleton(
            provider =>
                provider
                    .GetRequiredService<
                        IOptions<VirtuousOptions>>()
                    .Value);

        services.AddSingleton<
            IVirtuousWebhookMapper,
            VirtuousWebhookMapper>();

        services.AddHttpClient<
    IAplosAuthenticationService,
    AplosAuthenticationService>();

    services.AddHttpClient<
    IAplosApiClient,
    AplosApiClient>();

    services.AddScoped<
    IAplosTransactionService,
    AplosTransactionService>();

    services.Configure<IdempotencyOptions>(
    configuration.GetSection(
        IdempotencyOptions.SectionName));

    services.AddSingleton<
    IVirtuousGiftIdempotencyStore,
    SqliteVirtuousGiftIdempotencyStore>();

    services.AddSingleton<
    IVirtuousGiftTransactionMapper,
    VirtuousGiftTransactionMapper>();

    services.AddScoped<
    IVirtuousGiftService,
    VirtuousGiftService>();

        return services;
    }
}