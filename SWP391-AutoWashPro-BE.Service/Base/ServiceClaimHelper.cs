using Microsoft.AspNetCore.Http;

namespace SWP391_AutoWashPro_BE.Service.Base;

public class ServiceClaimHelper
{
    public static Guid GetRequiredUserId(IHttpContextAccessor httpContextAccessor)
    {
        return GetRequiredAccountId(httpContextAccessor, "UserId not found in token");
    }

    public static Guid GetRequiredAdminId(IHttpContextAccessor httpContextAccessor)
    {
        return GetRequiredAccountId(httpContextAccessor, "Admin ID claim is missing");
    }

    public static Guid GetRequiredAccountId(
        IHttpContextAccessor httpContextAccessor,
        string missingMessage = "Account ID claim is missing")
    {
        var accountIdValue = httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(claim => claim.Type == "id")?.Value;

        if (!Guid.TryParse(accountIdValue, out var accountId))
        {
            throw new UnauthorizedAccessException(missingMessage);
        }

        return accountId;
    }
}