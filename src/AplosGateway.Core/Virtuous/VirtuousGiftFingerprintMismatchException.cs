namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGiftFingerprintMismatchException
    : Exception
{
    public VirtuousGiftFingerprintMismatchException(
        long giftId)
        : base(
            $"Virtuous gift {giftId} does not match the originally received gift.")
    {
        GiftId = giftId;
    }

    public long GiftId { get; }
}