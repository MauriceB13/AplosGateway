namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGiftProcessingStateException
    : Exception
{
    public VirtuousGiftProcessingStateException(
        long giftId,
        VirtuousGiftProcessingStatus status)
        : base(
            $"Virtuous gift {giftId} cannot be processed because its current state is {status}.")
    {
        if (giftId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftId));
        }

        GiftId = giftId;
        Status = status;
    }

    public long GiftId { get; }

    public VirtuousGiftProcessingStatus Status { get; }
}