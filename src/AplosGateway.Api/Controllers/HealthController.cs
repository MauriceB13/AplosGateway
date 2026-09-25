using AplosGateway.Api.Configuration;
using AplosGateway.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AplosGateway.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly GatewayOptions _gatewayOptions;
    private readonly SecurityOptions _securityOptions;
    private readonly AplosOptions _aplosOptions;
    private readonly VirtuousOptions _virtuousOptions;
    private readonly TransactionMappingOptions _transactionMappingOptions;
    private readonly IdempotencyOptions _idempotencyOptions;

    public HealthController(
        IOptions<GatewayOptions> gatewayOptions,
        IOptions<SecurityOptions> securityOptions,
        IOptions<AplosOptions> aplosOptions,
        IOptions<VirtuousOptions> virtuousOptions,
        IOptions<TransactionMappingOptions> transactionMappingOptions,
        IOptions<IdempotencyOptions> idempotencyOptions)
    {
        _gatewayOptions = gatewayOptions.Value;
        _securityOptions = securityOptions.Value;
        _aplosOptions = aplosOptions.Value;
        _virtuousOptions = virtuousOptions.Value;
        _transactionMappingOptions = transactionMappingOptions.Value;
        _idempotencyOptions = idempotencyOptions.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            gateway = _gatewayOptions.Name,
            version = _gatewayOptions.Version
        });
    }

    [HttpGet("ready")]
    public IActionResult GetReady()
    {
        var isReady =
            !string.IsNullOrWhiteSpace(_securityOptions.ApiKey) &&
            !string.IsNullOrWhiteSpace(_aplosOptions.BaseUrl) &&
            !string.IsNullOrWhiteSpace(_aplosOptions.ClientId) &&
            !string.IsNullOrWhiteSpace(_aplosOptions.PrivateKey) &&
            _virtuousOptions.OrganizationId > 0 &&
            _transactionMappingOptions.DepositAccountNumber > 0 &&
            _transactionMappingOptions.IncomeAccountNumber > 0 &&
            _transactionMappingOptions.FundId > 0 &&
            !string.IsNullOrWhiteSpace(
                _idempotencyOptions.ConnectionString);

        if (!isReady)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    status = "NotReady"
                });
        }

        return Ok(new
        {
            status = "Ready"
        });
    }
}
