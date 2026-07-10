using System.Text.RegularExpressions;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.AiService;

public class IntentDetector
{
    private static readonly Regex GuidRegex =
        new(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

    public IntentDetectionResult Detect(string message)
    {
        var normalized = Normalize(message);
        var bookingId = TryExtractGuid(message);

        if (ContainsAny(normalized, "thong tin", "ho so", "tai khoan", "profile", "ca nhan"))
        {
            return new IntentDetectionResult(ChatIntent.UserProfile, bookingId);
        }

        if (ContainsAny(normalized, "diem", "hang", "loyalty", "gold", "silver", "point"))
        {
            return new IntentDetectionResult(ChatIntent.Loyalty, bookingId);
        }

        if (ContainsAny(normalized, "booking id", "chi tiet dat lich", "chi tiet booking", "booking detail") ||
            bookingId.HasValue)
        {
            return new IntentDetectionResult(ChatIntent.BookingDetail, bookingId);
        }

        if (ContainsAny(normalized, "lich su dat", "lich su booking", "dat lich", "booking", "don hang"))
        {
            return new IntentDetectionResult(ChatIntent.Booking, bookingId);
        }

        if (ContainsAny(normalized, "voucher", "ma giam", "coupon"))
        {
            return new IntentDetectionResult(ChatIntent.Voucher, bookingId);
        }

        if (ContainsAny(normalized, "khuyen mai", "promotion", "uu dai"))
        {
            return new IntentDetectionResult(ChatIntent.Promotion, bookingId);
        }

        if (ContainsAny(normalized, "chi nhanh gan nhat", "gan nhat", "nearest branch"))
        {
            return new IntentDetectionResult(ChatIntent.NearestBranch, bookingId);
        }

        if (ContainsAny(normalized, "top chi nhanh", "chi nhanh tot nhat", "top branch"))
        {
            return new IntentDetectionResult(ChatIntent.TopBranch, bookingId);
        }

        if (ContainsAny(normalized, "chi nhanh", "dia chi", "branch"))
        {
            return new IntentDetectionResult(ChatIntent.Branch, bookingId);
        }

        if (ContainsAny(normalized, "autowashpro", "gio mo cua", "huong dan", "faq", "lam sao", "cach"))
        {
            return new IntentDetectionResult(ChatIntent.Faq, bookingId);
        }

        return new IntentDetectionResult(ChatIntent.Unknown, bookingId);
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        return keywords.Any(source.Contains);
    }

    private static Guid? TryExtractGuid(string message)
    {
        var match = GuidRegex.Match(message);
        return match.Success && Guid.TryParse(match.Value, out var parsed) ? parsed : null;
    }

    private static string Normalize(string message)
    {
        return message.Trim().ToLowerInvariant();
    }
}

public sealed record IntentDetectionResult(ChatIntent Intent, Guid? BookingId);
