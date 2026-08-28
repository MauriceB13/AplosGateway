using AplosGateway.Api.Middleware;

namespace AplosGateway.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseGatewayPipeline(
        this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}