using Microsoft.AspNetCore.Authorization;
using SWP391_AutoWashPro_BE.Api.Controllers;
using SWP391_AutoWashPro_BE.Api.Extentions;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class RefundAuthorizationTests
{
    [Fact]
    public void BookingController_ShouldRequireCustomerPolicy()
    {
        var authorizeAttribute = typeof(BookingController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single();

        Assert.Equal(JwtExtensions.UserPolicy, authorizeAttribute.Policy);
    }

    [Fact]
    public void AdminController_ShouldRequireAdminPolicy()
    {
        var authorizeAttribute = typeof(AdminController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single();

        Assert.Equal(JwtExtensions.AdminPolicy, authorizeAttribute.Policy);
    }
}
