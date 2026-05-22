namespace SWP391_AutoWashPro_BE.Service.Admin;

public interface IService
{
    public Task<string> UpdateUserVerificationStatus(Guid userId);
}