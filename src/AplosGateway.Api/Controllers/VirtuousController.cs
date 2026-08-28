using AplosGateway.Core.Transactions;
using AplosGateway.Core.Virtuous;
using Microsoft.AspNetCore.Mvc;

namespace AplosGateway.Api.Controllers;

[ApiController]
[Route("api/virtuous")]
public sealed class VirtuousController
    : ControllerBase
{
    private readonly IVirtuousGiftService _giftService;
    private readonly IVirtuousGiftTransactionMapper _mapper;

    public VirtuousController(
        IVirtuousGiftService giftService,
        IVirtuousGiftTransactionMapper mapper)
    {
        _giftService = giftService;
        _mapper = mapper;
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

    [HttpPost("gift/preview")]
    public ActionResult<AplosTransactionRequest> PreviewGift(
        [FromBody] VirtuousGift gift)
    {
        var transaction =
            _mapper.Map(gift);

        return Ok(transaction);
    }
}