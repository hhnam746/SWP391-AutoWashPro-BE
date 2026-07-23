using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public static class RuleValidator
{
    private static readonly string[] InactiveEmailPlaceholders =
    {
        "{CustomerName}",
        "{VoucherName}",
        "{Discount}",
        "{ExpiresAt}",
        "{BookingUrl}"
    };

    public static void Validate(Request.RuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VoucherName))
        {
            throw new ArgumentException("VoucherName is required.");
        }

        if (request.VoucherName.Trim().Length > 200)
        {
            throw new ArgumentException("VoucherName cannot exceed 200 characters.");
        }

        if (request.DiscountValue <= 0)
        {
            throw new ArgumentException("DiscountValue must be greater than 0.");
        }

        if (request.DiscountType == DiscountType.Percentage && request.DiscountValue > 100)
        {
            throw new ArgumentException("Percentage DiscountValue cannot exceed 100.");
        }

        if (request.VoucherValidityDays <= 0)
        {
            throw new ArgumentException("VoucherValidityDays must be greater than 0.");
        }

        var requiresThreshold = request.TriggerType is
            PersonalizedVoucherTriggerType.InactiveCustomer or
            PersonalizedVoucherTriggerType.NoFirstBooking;

        if (requiresThreshold && (!request.ThresholdDays.HasValue || request.ThresholdDays.Value <= 0))
        {
            throw new ArgumentException("ThresholdDays must be greater than 0 for this trigger.");
        }

        if (!requiresThreshold && request.ThresholdDays.HasValue)
        {
            throw new ArgumentException("ThresholdDays is not supported for this trigger.");
        }

        if (request.TriggerType == PersonalizedVoucherTriggerType.InactiveCustomer && !request.SendEmail)
        {
            throw new ArgumentException("Inactive customer rules must send an email.");
        }

        if (request.SendInAppNotification &&
            (string.IsNullOrWhiteSpace(request.NotificationTitleTemplate) ||
             string.IsNullOrWhiteSpace(request.NotificationContentTemplate)))
        {
            throw new ArgumentException("Notification title and content templates are required.");
        }

        if (!request.SendEmail)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.EmailSubjectTemplate) ||
            string.IsNullOrWhiteSpace(request.EmailBodyTemplate))
        {
            throw new ArgumentException("Email subject and body templates are required.");
        }

        if (!string.IsNullOrWhiteSpace(request.CallToActionUrl) &&
            (!Uri.TryCreate(request.CallToActionUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new ArgumentException("CallToActionUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (request.TriggerType == PersonalizedVoucherTriggerType.InactiveCustomer)
        {
            if (string.IsNullOrWhiteSpace(request.CallToActionUrl))
            {
                throw new ArgumentException("Inactive customer rules require a CallToActionUrl.");
            }

            var missingPlaceholder = InactiveEmailPlaceholders
                .FirstOrDefault(x => !request.EmailBodyTemplate.Contains(x, StringComparison.Ordinal));

            if (missingPlaceholder != null)
            {
                throw new ArgumentException($"Inactive email template must contain {missingPlaceholder}.");
            }
        }
    }
}
