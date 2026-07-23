using SWP391_AutoWashPro_BE.Repository.Entities;
using AdminResponse = SWP391_AutoWashPro_BE.Service.Admin.Response;
using AuthRequest = SWP391_AutoWashPro_BE.Service.Auth.Request;
using UserResponse = SWP391_AutoWashPro_BE.Service.User.Response;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class CccdRemovalTests
{
    [Fact]
    public void PublicProfileContracts_AndEntity_DoNotExposeCccd()
    {
        Assert.Null(typeof(CustomerProfile).GetProperty("Cccd"));
        Assert.Null(typeof(AuthRequest.RegisterRequest).GetProperty("Cccd"));
        Assert.Null(typeof(UserResponse.ProfileData).GetProperty("Cccd"));
        Assert.Null(typeof(AdminResponse.ProfileData).GetProperty("Cccd"));
    }
}
