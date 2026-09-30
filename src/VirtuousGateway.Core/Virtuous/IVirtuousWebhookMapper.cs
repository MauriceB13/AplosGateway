namespace VirtuousGateway.Core.Virtuous;

public interface IVirtuousWebhookMapper
{
    VirtuousGift Map(
        VirtuousGiftWebhookRequest request);
}