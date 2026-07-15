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

        if (bookingId.HasValue || MatchesRule(normalized, ChatIntent.BookingDetail))
        {
            return new IntentDetectionResult(ChatIntent.BookingDetail, bookingId, normalized);
        }

        foreach (var rule in Rules)
        {
            if (rule.Intent == ChatIntent.BookingDetail)
            {
                continue;
            }

            if (ContainsAny(normalized, rule.Phrases))
            {
                return new IntentDetectionResult(rule.Intent, bookingId, normalized);
            }
        }

        return new IntentDetectionResult(ChatIntent.Unknown, bookingId, normalized);
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

public sealed record IntentDetectionResult(ChatIntent Intent, Guid? BookingId, string NormalizedMessage);
