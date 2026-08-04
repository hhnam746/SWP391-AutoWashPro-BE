using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Voucher;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }
    public async Task<Base.Response.PageResult<Response.VoucherResponse>> GetVoucher(Guid userId,int pageSize, int pageIndex)
    {
        var customer = await _dbContext.CustomerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
            throw new KeyNotFoundException("Customer not found");

        var query = _dbContext.Vouchers
            .Where(x => x.CustomerId == customer.Id);

        var totalItems = await query.CountAsync();

        query = query.OrderBy(x => x.Id);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selected = query.Select(y => new Response.VoucherResponse()
        {
            Id = y.Id,
            Code = y.Code,
            VoucherName = y.Name,
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

    public async Task<Base.Response.PageResult<Response.CustomerVoucherResponse>> GetMyVouchers(
        int pageSize,
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        return await GetCustomerVouchers(
            pageSize,
            pageIndex,
            availableOnly: false,
            cancellationToken);
    }

    public async Task<Base.Response.PageResult<Response.CustomerVoucherResponse>> GetAvailableVouchers(
        int pageSize,
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        return await GetCustomerVouchers(
            pageSize,
            pageIndex,
            availableOnly: true,
            cancellationToken);
    }

    private async Task<Base.Response.PageResult<Response.CustomerVoucherResponse>> GetCustomerVouchers(
        int pageSize,
        int pageIndex,
        bool availableOnly,
        CancellationToken cancellationToken)
    {
        if (pageSize < 1 || pageIndex < 1)
        {
            throw new ArgumentException("PageSize and PageIndex must be greater than 0.");
        }

        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContext);
        var customerId = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!customerId.HasValue)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        var query = _dbContext.Vouchers
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId.Value);
        if (availableOnly)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            query = query.Where(x =>
                x.Status == VoucherStatus.Active &&
                x.UsedAt == null &&
                x.ExpiresAt > nowUtc &&
                x.DiscountValue > 0);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.CustomerVoucherResponse
            {
                Id = x.Id,
                Code = x.Code,
                VoucherName = x.Name,
                RewardName = x.Reward != null ? x.Reward.Name : null,
                Source = x.PersonalizedVoucherIssuance != null
                    ? Response.VoucherSource.Personalized
                    : Response.VoucherSource.Reward,
                TriggerType = x.PersonalizedVoucherIssuance != null
                    ? x.PersonalizedVoucherIssuance.TriggerType
                    : null,
                CycleKey = x.PersonalizedVoucherIssuance != null
                    ? x.PersonalizedVoucherIssuance.CycleKey
                    : null,
                Status = x.Status,
                DiscountType = x.DiscountType,
                DiscountValue = x.DiscountValue,
                ExpiresAt = x.ExpiresAt,
                UsedAt = x.UsedAt
            })
            .ToListAsync(cancellationToken);

        return new Base.Response.PageResult<Response.CustomerVoucherResponse>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<Response.ValidateVoucherResponse> ValidateVoucher(Guid userId, Request.ValidateVoucherRequest request)
    {
        var customer = await _dbContext.CustomerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (customer == null)
            throw new KeyNotFoundException("Customer not found");

        var voucher = await _dbContext.Vouchers
            .Include(x => x.Reward)
            .FirstOrDefaultAsync(x =>
                x.Code == request.Code &&
                x.CustomerId == customer.Id);

        if (voucher == null)
            throw new KeyNotFoundException("Voucher not found");

        if (voucher.Status == VoucherStatus.Reserved)
            throw new InvalidOperationException("Voucher is reserved by another booking.");

        if (voucher.Status == VoucherStatus.Used || voucher.UsedAt != null)
            throw new InvalidOperationException("Voucher already used.");

        if (voucher.Status == VoucherStatus.Expired ||
            voucher.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Voucher expired.");

        if (voucher.Status != VoucherStatus.Active)
            throw new InvalidOperationException("Voucher is not available.");

        if (voucher.DiscountValue <= 0)
            throw new InvalidOperationException("Voucher has no discount value.");

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
            VoucherName = voucher.Name,
            RewardName = voucher.Reward?.Name,
            IsValid = true,
            Message = "Voucher is valid",
            DiscountAmount = discountAmount,
            FinalAmount = request.TotalAmount - discountAmount
        }; 
        
        return result;
    }
}
