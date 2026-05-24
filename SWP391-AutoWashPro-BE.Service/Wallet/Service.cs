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
            throw new Exception("User not found");
        var query = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == user.Id);
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
            throw new Exception("User not found");
        
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == user.Id);
        if (wallet == null)
        {
            throw new Exception("Wallet not found");
        }
        wallet.Balance += request.Balance;
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