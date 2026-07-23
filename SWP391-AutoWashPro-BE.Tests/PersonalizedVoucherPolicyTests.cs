using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class PersonalizedVoucherPolicyTests
{
    [Fact]
    public void CycleKeys_ContainTheBusinessCycleInputs()
    {
        var timestamp = new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);
        var tierId = Guid.Parse("70ec817f-9594-4f85-b7a9-a1e79732e5a4");

        Assert.Equal("BIRTHDAY:2026", PersonalizationPolicy.CreateBirthdayCycleKey(2026));
        Assert.Equal(
            $"INACTIVE:30:{timestamp.UtcTicks}",
            PersonalizationPolicy.CreateInactiveCycleKey(30, timestamp));
        Assert.Equal(
            $"WELCOME:{timestamp.UtcTicks}",
            PersonalizationPolicy.CreateWelcomeCycleKey(timestamp));
        Assert.Equal(
            $"NO_FIRST_BOOKING:7:{timestamp.UtcTicks}",
            PersonalizationPolicy.CreateNoFirstBookingCycleKey(7, timestamp));
        Assert.Equal(
            $"TIER_UPGRADE:{tierId}",
            PersonalizationPolicy.CreateTierUpgradeCycleKey(tierId));
    }

    [Theory]
    [InlineData(2025, 2, 28, true)]
    [InlineData(2025, 3, 1, false)]
    [InlineData(2024, 2, 28, false)]
    [InlineData(2024, 2, 29, true)]
    public void LeapDayBirthday_UsesFebruary28OnlyInNonLeapYears(
        int year,
        int month,
        int day,
        bool expected)
    {
        var result = PersonalizationPolicy.IsBirthday(
            new DateOnly(2000, 2, 29),
            new DateOnly(year, month, day));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AcquisitionTrigger_WelcomeHasPrecedence()
    {
        Assert.Equal(
            PersonalizedVoucherTriggerType.Welcome,
            PersonalizationPolicy.ChooseAcquisitionTrigger(true, true));
        Assert.Equal(
            PersonalizedVoucherTriggerType.NoFirstBooking,
            PersonalizationPolicy.ChooseAcquisitionTrigger(false, true));
        Assert.Null(PersonalizationPolicy.ChooseAcquisitionTrigger(false, false));
    }

    [Fact]
    public void InactiveTemplate_RequiresOfferExpiryAndCallToActionPlaceholders()
    {
        var request = CreateInactiveRuleRequest(
            "Hello {CustomerName}: {VoucherName}, {Discount}, expires {ExpiresAt}.");

        var exception = Assert.Throws<ArgumentException>(() => RuleValidator.Validate(request));
        Assert.Contains("{BookingUrl}", exception.Message);

        request.EmailBodyTemplate += " Book at {BookingUrl}. Code {VoucherCode}.";
        RuleValidator.Validate(request);
    }

    [Fact]
    public void RuleValidator_RejectsBlankVoucherName()
    {
        var request = CreateInactiveRuleRequest(
            "Hello {CustomerName}: {VoucherName}, {Discount}, expires {ExpiresAt}. Book at {BookingUrl}.");
        request.VoucherName = " ";

        var exception = Assert.Throws<ArgumentException>(() => RuleValidator.Validate(request));

        Assert.Contains("VoucherName", exception.Message);
    }

    [Theory]
    [InlineData(DiscountType.FixedAmount, 0)]
    [InlineData(DiscountType.Percentage, 0)]
    [InlineData(DiscountType.Percentage, 101)]
    public void RuleValidator_RejectsInvalidDiscount(
        DiscountType discountType,
        decimal discountValue)
    {
        var request = CreateInactiveRuleRequest(
            "Hello {CustomerName}: {VoucherName}, {Discount}, expires {ExpiresAt}. Book at {BookingUrl}.");
        request.DiscountType = discountType;
        request.DiscountValue = discountValue;

        Assert.Throws<ArgumentException>(() => RuleValidator.Validate(request));
    }

    [Fact]
    public void TemplateRenderer_RendersAllSupportedValuesAndEncodesHtmlValues()
    {
        var rendered = TemplateRenderer.Render(
            "{CustomerName}|{VoucherName}|{PromotionName}|{Discount}|{VoucherCode}|{ExpiresAt}|{BookingUrl}",
            "A <B>",
            "Summer & Wash",
            DiscountType.Percentage,
            15,
            "PV-123",
            "31/07/2026 23:59",
            "https://example.com/book?a=1&b=2",
            true);

        Assert.Equal(
            "A &lt;B&gt;|Summer &amp; Wash|Summer &amp; Wash|15%|PV-123|31/07/2026 23:59|https://example.com/book?a=1&amp;b=2",
            rendered);
    }

    private static Request.RuleRequest CreateInactiveRuleRequest(string emailBody)
    {
        return new Request.RuleRequest
        {
            VoucherName = "Inactive Customer Voucher",
            TriggerType = PersonalizedVoucherTriggerType.InactiveCustomer,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15,
            ThresholdDays = 30,
            VoucherValidityDays = 14,
            IsActive = true,
            SendEmail = true,
            EmailSubjectTemplate = "We miss you, {CustomerName}",
            EmailBodyTemplate = emailBody,
            CallToActionUrl = "https://example.com/booking"
        };
    }

}
