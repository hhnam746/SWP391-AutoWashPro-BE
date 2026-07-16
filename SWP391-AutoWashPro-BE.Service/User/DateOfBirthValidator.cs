namespace SWP391_AutoWashPro_BE.Service.User;

internal static class DateOfBirthValidator
{
    public static void EnsureValid(DateOnly dateOfBirth)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth > todayUtc)
        {
            throw new ArgumentException("Date of birth cannot be in the future.");
        }
    }
}
