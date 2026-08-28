using AplosGateway.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AplosGateway.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly GatewayOptions _options;

    public HealthController(IOptions<GatewayOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            gateway = _options.Name,
            version = _options.Version
        });
    }
}
