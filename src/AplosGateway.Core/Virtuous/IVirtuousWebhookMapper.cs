namespace AplosGateway.Core.Virtuous;

public interface IVirtuousWebhookMapper
{
    VirtuousGift Map(
        VirtuousGiftWebhookRequest request);
}