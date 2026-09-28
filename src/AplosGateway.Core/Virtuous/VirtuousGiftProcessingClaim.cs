namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGiftProcessingClaim
{
    public required VirtuousGiftProcessingRecord Record { get; init; }

    public bool ShouldProcess { get; init; }
}