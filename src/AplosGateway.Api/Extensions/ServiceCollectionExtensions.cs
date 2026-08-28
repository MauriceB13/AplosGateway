using AplosGateway.Api.Configuration;

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

        services.AddMemoryCache();

        return services;
    }
}
