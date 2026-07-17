using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.AiService;

public class IntentDetector
{
    private static readonly Regex GuidRegex =
        new(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);
    private static readonly Regex DateRegex =
        new(@"\b(?<day>\d{1,2})[/-](?<month>\d{1,2})[/-](?<year>\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex BookingCountRegex =
        new(@"\b(?<count>\d{1,3})\s+booking\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LicensePlateRegex =
        new(@"\b\d{2}[a-z]\d?(?:[-.\s]?\d){4,7}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PunctuationRegex = new(@"[^\p{L}\p{N}\s]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private static readonly IntentRule[] Rules =
    [
        new(ChatIntent.UserProfile,
        [
            "thong tin cua ban",
            "thong tin cua toi",
            "thong tin tai khoan",
            "thong tin ca nhan",
            "ho so ca nhan",
            "ho so cua toi",
            "tai khoan cua toi",
            "profile cua toi",
            "thong tin",
            "ho so",
            "tai khoan",
            "profile",
            "ca nhan",
            "ban la ai",
            "gioi thieu ban than"
        ]),
        new(ChatIntent.Loyalty,
        [
            "hang thanh vien",
            "diem thuong",
            "loyalty",
            "gold",
            "silver",
            "point",
            "diem",
            "hang"
        ]),
        new(ChatIntent.BookingDetail,
        [
            "booking id",
            "chi tiet dat lich",
            "chi tiet booking",
            "booking detail",
            "ma booking",
            "ma dat lich"
        ]),
        new(ChatIntent.Booking,
        [
            "lich su dat lich",
            "lich su booking",
            "booking sap toi",
            "dat lich",
            "booking",
            "don hang"
        ]),
        new(ChatIntent.Voucher,
        [
            "ma giam gia",
            "ma giam",
            "voucher",
            "coupon"
        ]),
        new(ChatIntent.Promotion,
        [
            "khuyen mai",
            "uu dai",
            "promotion"
        ]),
        new(ChatIntent.NearestBranch,
        [
            "chi nhanh gan nhat",
            "branch gan nhat",
            "gan nhat",
            "nearest branch"
        ]),
        new(ChatIntent.TopBranch,
        [
            "top chi nhanh",
            "chi nhanh tot nhat",
            "branch tot nhat",
            "top branch"
        ]),
        new(ChatIntent.Branch,
        [
            "danh sach chi nhanh",
            "dia chi chi nhanh",
            "chi nhanh",
            "dia chi",
            "branch"
        ]),
        new(ChatIntent.Faq,
        [
            "xin chao",
            "hello",
            "hi",
            "chao",
            "alo",
            "autowashpro",
            "gio mo cua",
            "huong dan su dung",
            "huong dan",
            "lam sao",
            "cach",
            "ban co the lam gi",
            "ban ho tro gi",
            "faq"
        ])
    ];

    public IntentDetectionResult Detect(string message)
    {
        var normalized = Normalize(message);
        var bookingId = TryExtractGuid(message);
        var bookingDate = TryExtractBookingDate(message);
        var requestedBookingCount = TryExtractRequestedBookingCount(message);
        var licensePlate = TryExtractLicensePlate(normalized);
        var bookingStatus = TryExtractBookingStatus(normalized);
        var hasDetailHint = MatchesRule(normalized, ChatIntent.BookingDetail) ||
                            (ContainsAny(normalized, ["chi tiet"]) && ContainsAny(normalized, ["booking", "dat lich"]));
        var hasBranchHint = ContainsAny(normalized, ["chi nhanh", "branch"]);
        var hasLicensePlateHint = licensePlate is not null || ContainsAny(normalized, ["bien so", "xe"]);
        var hasStatusHint = bookingStatus.HasValue ||
                            ContainsAny(normalized,
                                ["da huy", "huy", "cancel", "cancelled", "dang xac nhan", "xac nhan", "confirmed",
                                 "dang thuc hien", "in progress", "hoan thanh", "completed", "sap toi"]);

        if (bookingId.HasValue)
        {
            return new IntentDetectionResult(
                ChatIntent.BookingDetail,
                bookingId,
                normalized,
                bookingDate,
                requestedBookingCount,
                licensePlate,
                bookingStatus,
                hasDetailHint,
                hasBranchHint,
                hasLicensePlateHint,
                hasStatusHint);
        }

        if (hasDetailHint)
        {
            return new IntentDetectionResult(
                ChatIntent.BookingDetail,
                bookingId,
                normalized,
                bookingDate,
                requestedBookingCount,
                licensePlate,
                bookingStatus,
                hasDetailHint,
                hasBranchHint,
                hasLicensePlateHint,
                hasStatusHint);
        }

        foreach (var rule in Rules)
        {
            if (rule.Intent == ChatIntent.BookingDetail)
            {
                continue;
            }

            if (ContainsAny(normalized, rule.Phrases))
            {
                return new IntentDetectionResult(
                    rule.Intent,
                    bookingId,
                    normalized,
                    bookingDate,
                    requestedBookingCount,
                    licensePlate,
                    bookingStatus,
                    hasDetailHint,
                    hasBranchHint,
                    hasLicensePlateHint,
                    hasStatusHint);
            }
        }

        if (licensePlate is not null && hasLicensePlateHint)
        {
            return new IntentDetectionResult(
                ChatIntent.Booking,
                bookingId,
                normalized,
                bookingDate,
                requestedBookingCount,
                licensePlate,
                bookingStatus,
                hasDetailHint,
                hasBranchHint,
                hasLicensePlateHint,
                hasStatusHint);
        }

        return new IntentDetectionResult(
            ChatIntent.Unknown,
            bookingId,
            normalized,
            bookingDate,
            requestedBookingCount,
            licensePlate,
            bookingStatus,
            hasDetailHint,
            hasBranchHint,
            hasLicensePlateHint,
            hasStatusHint);
    }

    private static bool MatchesRule(string source, ChatIntent intent)
    {
        var rule = Rules.FirstOrDefault(x => x.Intent == intent);
        return rule is not null && ContainsAny(source, rule.Phrases);
    }

    private static bool ContainsAny(string source, IReadOnlyCollection<string> keywords)
    {
        var paddedSource = $" {source} ";
        return keywords.Any(keyword => paddedSource.Contains($" {keyword} ", StringComparison.Ordinal));
    }

    private static Guid? TryExtractGuid(string message)
    {
        var match = GuidRegex.Match(message);
        return match.Success && Guid.TryParse(match.Value, out var parsed) ? parsed : null;
    }

    private static DateOnly? TryExtractBookingDate(string message)
    {
        var match = DateRegex.Match(message);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["day"].Value, out var day) ||
            !int.TryParse(match.Groups["month"].Value, out var month) ||
            !int.TryParse(match.Groups["year"].Value, out var year))
        {
            return null;
        }

        return DateOnly.TryParseExact(
            $"{day:D2}/{month:D2}/{year:D4}",
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate)
            ? parsedDate
            : null;
    }

    private static int? TryExtractRequestedBookingCount(string message)
    {
        var match = BookingCountRegex.Match(message);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups["count"].Value, out var parsedCount) && parsedCount > 0
            ? parsedCount
            : null;
    }

    private static string? TryExtractLicensePlate(string normalizedMessage)
    {
        var match = LicensePlateRegex.Match(normalizedMessage);
        if (!match.Success)
        {
            return null;
        }

        var sanitized = SanitizeAlphaNumeric(match.Value);
        return sanitized.Length is >= 5 and <= 12 ? sanitized : null;
    }

    private static BookingStatus? TryExtractBookingStatus(string normalizedMessage)
    {
        if (ContainsAny(normalizedMessage, ["da huy", "huy", "cancel", "cancelled"]))
        {
            return BookingStatus.Cancelled;
        }

        if (ContainsAny(normalizedMessage, ["dang thuc hien", "in progress"]))
        {
            return BookingStatus.InProgress;
        }

        if (ContainsAny(normalizedMessage, ["hoan thanh", "completed"]))
        {
            return BookingStatus.Completed;
        }

        if (ContainsAny(normalizedMessage, ["dang xac nhan", "xac nhan", "confirmed", "sap toi"]))
        {
            return BookingStatus.Confirmed;
        }

        return null;
    }

    private static string SanitizeAlphaNumeric(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string Normalize(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var lowercase = message.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(lowercase.Length);

        foreach (var character in lowercase)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                'đ' => 'd',
                _ => character
            });
        }

        var withoutPunctuation = PunctuationRegex.Replace(builder.ToString(), " ");
        return WhitespaceRegex.Replace(withoutPunctuation, " ").Trim();
    }

    private sealed record IntentRule(ChatIntent Intent, string[] Phrases);
}

public sealed record IntentDetectionResult(
    ChatIntent Intent,
    Guid? BookingId,
    string NormalizedMessage,
    DateOnly? BookingDate,
    int? RequestedBookingCount,
    string? LicensePlate,
    BookingStatus? BookingStatus,
    bool HasDetailHint,
    bool HasBranchHint,
    bool HasLicensePlateHint,
    bool HasStatusHint);
