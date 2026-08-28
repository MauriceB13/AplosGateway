using AplosGateway.Api.Configuration;
using AplosGateway.Core.Security;
using AplosGateway.Infrastructure.Security;
using AplosGateway.Core.Authentication;
using AplosGateway.Infrastructure.Authentication;
using AplosGateway.Core.Configuration;

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

        return services;
    }
}