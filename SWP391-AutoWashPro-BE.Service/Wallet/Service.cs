using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Wallet;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<Response.GetWalleResponse> GetUserWallet()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new InvalidOperationException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");

        var query = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        
        if (query == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }
        
        var result = new Response.GetWalleResponse
        {
            Id = query.Id,
            Balance = query.Balance,
            Currency = "VND"
        };
        return result;
    }

    public async Task<Response.WalletTopupResponse> TopupUserWallet(Request.WalletTopupRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new InvalidOperationException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }
        wallet.Balance += request.Balance;

        var topUpTransaction = new Repository.Entities.Transaction
        {
            Amount = request.Balance,
            Type = Repository.Enums.TransactionType.WalletTopup,
            Description = "Wallet top-up",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        await _dbContext.Transactions.AddAsync(topUpTransaction);
        await _dbContext.SaveChangesAsync();

        var result = new Response.WalletTopupResponse
        {
            Id = wallet.Id,
            Balance = wallet.Balance,
            Message = "Wallet topped up successfully"
        };
        return result;
    }
}