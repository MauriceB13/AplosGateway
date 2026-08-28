using System.ComponentModel.DataAnnotations;

namespace AplosGateway.Core.Virtuous;

public sealed class VirtuousGift
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }

    [Required]
    public string ContactName { get; set; } = string.Empty;

    public DateTime GiftDateUtc { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    public string Project { get; set; } = string.Empty;

    public string ProjectCode { get; set; } = string.Empty;

    public string Segment { get; set; } = string.Empty;
}