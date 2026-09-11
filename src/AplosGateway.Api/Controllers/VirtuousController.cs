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
    private readonly IVirtuousGiftTransactionMapper _transactionMapper;
    private readonly IVirtuousWebhookMapper _webhookMapper;

    public VirtuousController(
        IVirtuousGiftService giftService,
        IVirtuousGiftTransactionMapper transactionMapper,
        IVirtuousWebhookMapper webhookMapper)
    {
        _giftService = giftService;
        _transactionMapper = transactionMapper;
        _webhookMapper = webhookMapper;
    }

   [HttpPost("gift")]
public async Task<IActionResult> ProcessGift(
    [FromBody] VirtuousGiftWebhookRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var gift =
            _webhookMapper.Map(request);

        var result =
            await _giftService.ProcessGiftAsync(
                gift,
                cancellationToken);

        return Content(
            result,
            "application/json");
    }
    catch (VirtuousWebhookValidationException exception)
    {
        return BadRequest(
            new
            {
                error = exception.Message
            });
    }
}

[HttpPost("gift/preview")]
public ActionResult<AplosTransactionRequest> PreviewGift(
    [FromBody] VirtuousGiftWebhookRequest request)
{
    try
    {
        var gift =
            _webhookMapper.Map(request);

        var transaction =
            _transactionMapper.Map(gift);

        return Ok(transaction);
    }
    catch (VirtuousWebhookValidationException exception)
    {
        return BadRequest(
            new
            {
                error = exception.Message
            });
    }
}
}