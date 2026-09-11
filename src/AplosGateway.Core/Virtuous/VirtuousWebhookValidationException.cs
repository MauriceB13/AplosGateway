namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousWebhookValidationException
    : Exception
{
    public VirtuousWebhookValidationException(
        string message)
        : base(message)
    {
    }
}