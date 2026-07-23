using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Constants;

public static class PersonalizedVoucherConfigKeys
{
    public const string Birthday = "PersonalizedVoucher.Birthday.Enabled";
    public const string InactiveCustomer = "PersonalizedVoucher.InactiveCustomer.Enabled";
    public const string Welcome = "PersonalizedVoucher.Welcome.Enabled";
    public const string NoFirstBooking = "PersonalizedVoucher.NoFirstBooking.Enabled";
    public const string TierUpgrade = "PersonalizedVoucher.TierUpgrade.Enabled";

    public static string ForTrigger(PersonalizedVoucherTriggerType triggerType) => triggerType switch
    {
        PersonalizedVoucherTriggerType.Birthday => Birthday,
        PersonalizedVoucherTriggerType.InactiveCustomer => InactiveCustomer,
        PersonalizedVoucherTriggerType.Welcome => Welcome,
        PersonalizedVoucherTriggerType.NoFirstBooking => NoFirstBooking,
        PersonalizedVoucherTriggerType.TierUpgrade => TierUpgrade,
        _ => throw new ArgumentOutOfRangeException(nameof(triggerType), triggerType, null)
    };
}
