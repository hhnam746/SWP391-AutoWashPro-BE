using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public static class PersonalizationPolicy
{
    private const string BirthdayCyclePrefix = "BIRTHDAY";
    private const string InactiveCyclePrefix = "INACTIVE";
    private const string WelcomeCyclePrefix = "WELCOME";
    private const string NoFirstBookingCyclePrefix = "NO_FIRST_BOOKING";
    private const string TierUpgradeCyclePrefix = "TIER_UPGRADE";

    public static bool IsBirthday(DateOnly dateOfBirth, DateOnly localDate)
    {
        if (dateOfBirth.Month == 2 && dateOfBirth.Day == 29 && !DateTime.IsLeapYear(localDate.Year))
        {
            return localDate.Month == 2 && localDate.Day == 28;
        }

        return dateOfBirth.Month == localDate.Month && dateOfBirth.Day == localDate.Day;
    }

    public static string CreateBirthdayCycleKey(int year) => $"{BirthdayCyclePrefix}:{year}";

    public static string CreateInactiveCycleKey(int thresholdDays, DateTimeOffset lastLoginAt) =>
        $"{InactiveCyclePrefix}:{thresholdDays}:{lastLoginAt.UtcTicks}";

    public static string CreateWelcomeCycleKey(DateTimeOffset verifiedAt) =>
        $"{WelcomeCyclePrefix}:{verifiedAt.UtcTicks}";

    public static string CreateNoFirstBookingCycleKey(int thresholdDays, DateTimeOffset createdAt) =>
        $"{NoFirstBookingCyclePrefix}:{thresholdDays}:{createdAt.UtcTicks}";

    public static string CreateTierUpgradeCycleKey(Guid tierId) => $"{TierUpgradeCyclePrefix}:{tierId}";

    public static bool IsAcquisitionTrigger(PersonalizedVoucherTriggerType triggerType) =>
        triggerType is PersonalizedVoucherTriggerType.Welcome or
            PersonalizedVoucherTriggerType.NoFirstBooking;

    public static PersonalizedVoucherTriggerType? ChooseAcquisitionTrigger(
        bool isWelcomeEligible,
        bool isNoFirstBookingEligible)
    {
        if (isWelcomeEligible)
        {
            return PersonalizedVoucherTriggerType.Welcome;
        }

        return isNoFirstBookingEligible
            ? PersonalizedVoucherTriggerType.NoFirstBooking
            : null;
    }
}
