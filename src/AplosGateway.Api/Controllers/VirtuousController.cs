using AplosGateway.Core.Virtuous;
using Microsoft.AspNetCore.Mvc;

namespace AplosGateway.Api.Controllers;

[ApiController]
[Route("api/virtuous")]
public sealed class VirtuousController
    : ControllerBase
{
    private readonly IVirtuousGiftService _giftService;

    public VirtuousController(
        IVirtuousGiftService giftService)
    {
        _giftService = giftService;
    }

    [HttpPost("gift")]
    public async Task<IActionResult> ProcessGift(
        [FromBody] VirtuousGift gift,
        CancellationToken cancellationToken)
    {
        var result =
            await _giftService.ProcessGiftAsync(
                gift,
                cancellationToken);

        return Content(
            result,
            "application/json");
    }
}