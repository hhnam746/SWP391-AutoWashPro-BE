using System.Globalization;
using System.Net;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public static class TemplateRenderer
{
    public static string Render(
        string template,
        string customerName,
        string voucherName,
        DiscountType discountType,
        decimal discountValue,
        string voucherCode,
        string expiresAt,
        string bookingUrl,
        bool htmlEncodeValues)
    {
        var values = new Dictionary<string, string>
        {
            ["{CustomerName}"] = customerName,
            ["{VoucherName}"] = voucherName,
            ["{PromotionName}"] = voucherName,
            ["{Discount}"] = FormatDiscount(discountType, discountValue),
            ["{VoucherCode}"] = voucherCode,
            ["{ExpiresAt}"] = expiresAt,
            ["{BookingUrl}"] = bookingUrl
        };

        var rendered = template;
        foreach (var pair in values)
        {
            var value = htmlEncodeValues ? WebUtility.HtmlEncode(pair.Value) : pair.Value;
            rendered = rendered.Replace(pair.Key, value, StringComparison.Ordinal);
        }

        return rendered;
    }

    public static string FormatDiscount(DiscountType discountType, decimal discountValue)
    {
        var value = discountValue.ToString("0.##", CultureInfo.InvariantCulture);
        return discountType == DiscountType.Percentage ? $"{value}%" : $"{value} VND";
    }
}
