using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Voucher;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Base.Response.PageResult<Response.VoucherResponse>> GetVoucher(Guid userId,int pageSize, int pageIndex)
    {
        var customer = await _dbContext.CustomerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
            throw new Exception("Customer not found");

        var query = _dbContext.Vouchers
            .Where(x => x.CustomerId == customer.Id);

        var totalItems = await query.CountAsync();

        query = query.OrderBy(x => x.Id);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selected = query.Select(y => new Response.VoucherResponse()
        {
            Id = y.Id,
            Code = y.Code,
            RewardName = y.Reward != null ? y.Reward.Name : null,
            Status = y.Status,
            DiscountType = y.DiscountType,
            DiscountValue = y.DiscountValue,
            ExpiresAt = y.ExpiresAt,
            UsedAt = y.UsedAt
        });
        var listResult = await selected.ToListAsync();

        var result = new Base.Response.PageResult<Response.VoucherResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return result;
    }

    public async Task<Response.ValidateVoucherResponse> ValidateVoucher(Guid userId, Request.ValidateVoucherRequest request)
    {
        var customer = await _dbContext.CustomerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
            throw new Exception("Customer not found");

        var voucher = await _dbContext.Vouchers
            .Include(x => x.Reward)
            .FirstOrDefaultAsync(x =>
                x.Code == request.Code &&
                x.CustomerId == customer.Id);

        if (voucher == null)
            throw new Exception("Voucher not found");

        if (voucher.Status != VoucherStatus.Active)
            throw new Exception("Voucher is inactive");

        if (voucher.ExpiresAt < DateTimeOffset.UtcNow)
            throw new Exception("Voucher expired");

        if (voucher.UsedAt != null)
            throw new Exception("Voucher already used");

        if (voucher.DiscountValue <= 0)
            throw new Exception("Voucher has no discount value");

        decimal discountAmount;

        if (voucher.DiscountType == DiscountType.Percentage)
        {
            discountAmount =
                request.TotalAmount *
                voucher.DiscountValue / 100;
        }
        else
        {
            discountAmount = voucher.DiscountValue;
        }

        if (discountAmount > request.TotalAmount)
        {
            discountAmount = request.TotalAmount;
        }
        
        var result = new Response.ValidateVoucherResponse()
        {
            VoucherId = voucher.Id,
            Code = voucher.Code,
            RewardName = voucher.Reward?.Name,
            IsValid = true,
            Message = "Voucher is valid",
            DiscountAmount = discountAmount,
            FinalAmount = request.TotalAmount - discountAmount
        }; 
        
        return result;
    }
}
