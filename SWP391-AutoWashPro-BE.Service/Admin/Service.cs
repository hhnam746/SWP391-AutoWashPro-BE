using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> UpdateUserVerificationStatus(Guid userId)
    {
        var targetUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (targetUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (targetUser.Role != UserRole.Customer)
        {
            throw new InvalidOperationException("Only customer accounts can be verified.");
        }

        if (targetUser.Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Only active users can be verified.");
        }

        if (targetUser.isVerify)
        {
            return "User is already verified.";
        }

        targetUser.isVerify = true;
        targetUser.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        return "Verify user successfully";
    }
}
