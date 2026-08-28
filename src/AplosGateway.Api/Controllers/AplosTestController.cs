using AplosGateway.Core.Aplos;
using Microsoft.AspNetCore.Mvc;

namespace AplosGateway.Api.Controllers;

[ApiController]
[Route("api/aplos/test")]
public sealed class AplosTestController
    : ControllerBase
{
    private readonly IAplosApiClient _aplosApiClient;

    public AplosTestController(
        IAplosApiClient aplosApiClient)
    {
        _aplosApiClient = aplosApiClient;
    }

    [HttpGet("funds")]
    public async Task<IActionResult> GetFunds(
        CancellationToken cancellationToken)
    {
        var result =
            await _aplosApiClient.GetAsync(
                "funds",
                cancellationToken);

        return Content(
            result,
            "application/json");
    }

    [HttpGet("funds/{fundId:int}")]
    public async Task<IActionResult> GetFund(
        int fundId,
        CancellationToken cancellationToken)
    {
        var result =
            await _aplosApiClient.GetAsync(
                $"funds/{fundId}",
                cancellationToken);

        return Content(
            result,
            "application/json");
    }
}