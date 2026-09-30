using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VirtuousGateway.Core.Virtuous;

public static class VirtuousGiftFingerprint
{
    public static string Create(
        VirtuousGift gift)
    {
        ArgumentNullException.ThrowIfNull(gift);

        var builder =
            new StringBuilder();

        AppendField(
            builder,
            "Id",
            gift.Id.ToString(
                CultureInfo.InvariantCulture));

        AppendField(
            builder,
            "ContactName",
            gift.ContactName);

        AppendField(
            builder,
            "GiftDateUtc",
            NormalizeDate(
                gift.GiftDateUtc));

        AppendField(
            builder,
            "Amount",
            gift.Amount.ToString(
                CultureInfo.InvariantCulture));

        AppendField(
            builder,
            "Project",
            gift.Project);

        AppendField(
            builder,
            "ProjectCode",
            gift.ProjectCode);

        AppendField(
            builder,
            "Segment",
            gift.Segment);

        var bytes =
            Encoding.UTF8.GetBytes(
                builder.ToString());

        var hash =
            SHA256.HashData(
                bytes);

        return Convert.ToHexString(
            hash);
    }

    private static string NormalizeDate(
        DateTime value)
    {
        var utc =
            value.Kind switch
            {
                DateTimeKind.Utc =>
                    value,

                DateTimeKind.Local =>
                    value.ToUniversalTime(),

                DateTimeKind.Unspecified =>
                    DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc),

                _ =>
                    value
            };

        return utc.ToString(
            "O",
            CultureInfo.InvariantCulture);
    }

    private static void AppendField(
        StringBuilder builder,
        string name,
        string? value)
    {
        value ??=
            string.Empty;

        builder.Append(
            name);

        builder.Append(
            ':');

        builder.Append(
            value.Length.ToString(
                CultureInfo.InvariantCulture));

        builder.Append(
            ':');

        builder.Append(
            value);
    }
}