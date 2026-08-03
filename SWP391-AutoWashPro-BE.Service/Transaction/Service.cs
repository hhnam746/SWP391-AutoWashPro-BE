using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Transaction;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task<Repository.Entities.CustomerProfile> GetRequiredCustomerProfileAsync()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContextAccessor);

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Exception note: phát sinh khi access token hợp lệ về mặt kỹ thuật nhưng user không còn tồn tại trong hệ thống.
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        // Exception note: phát sinh khi user đã tồn tại nhưng chưa có hồ sơ khách hàng tương ứng hoặc dữ liệu bị thiếu.
        if (customerProfile == null)
            throw new KeyNotFoundException("Customer profile not found.");

        return customerProfile;
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        // Exception note: phát sinh khi client truyền page/pageSize không hợp lệ.
        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be greater than 0.");

        // Exception note: phát sinh khi client truyền page/pageSize không hợp lệ.
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than 0.");
    }


    public async Task<Response.GetTransactionResponse> GetTransactions(Request.GetTransactionsRequest request)
    {
        var customerProfile = await GetRequiredCustomerProfileAsync();
        ValidatePagination(request.PageIndex, request.PageSize);

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.CustomerId == customerProfile.Id);

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var descriptionKeyword = request.Description.Trim();
            query = query.Where(x => x.Description != null && x.Description.Contains(descriptionKeyword));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;
            query = query.Where(x => x.TransactionDate >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.TransactionDate <= toDate);
        }

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new Response.GetTransactionItems
            {
                TransactionId = x.Id,
                CustomerId = x.CustomerId,
                BookingId = x.BookingId,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                TransactionDate = x.TransactionDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new Response.GetTransactionResponse
        {
            Transactions = transactions,
            Pagination = new Response.PaginationResponse
            {
                Page = request.PageIndex,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<Response.GetTransactionItems> GetTransactionById(Guid id)
    {
        var customerProfile = await GetRequiredCustomerProfileAsync();

        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerProfile.Id)
            .Select(x => new Response.GetTransactionItems
            {
                TransactionId = x.Id,
                CustomerId = x.CustomerId,
                BookingId = x.BookingId,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                TransactionDate = x.TransactionDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
            throw new KeyNotFoundException("Transaction not found.");

        return transaction;
    }

    public async Task<Response.GetTransactionResponse> GetTransactionsV2(Request.GetTransactionsRequest request)
    {
        var customerProfile = await GetRequiredCustomerProfileAsync();
        ValidatePagination(request.PageIndex, request.PageSize);

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.CustomerId == customerProfile.Id);

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var descriptionKeyword = request.Description.Trim();
            query = query.Where(transaction =>
                transaction.Description != null &&
                transaction.Description.Contains(descriptionKeyword));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == request.Type.Value);
        }

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;
            query = query.Where(transaction => transaction.TransactionDate >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(transaction => transaction.TransactionDate <= toDate);
        }

        var totalCount = await query.CountAsync();

        var selectedQuery = query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => new Response.GetTransactionItems
            {
                TransactionId = transaction.Id,
                CustomerId = transaction.CustomerId,
                BookingId = transaction.BookingId,
                Amount = transaction.Amount,
                Type = transaction.Type,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });

        var transactions = await selectedQuery
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new Response.GetTransactionResponse
        {
            Transactions = transactions,
            Pagination = new Response.PaginationResponse
            {
                Page = request.PageIndex,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<Response.GetTransactionItems> GetTransactionByIdV2(Guid id)
    {
        var customerProfile = await GetRequiredCustomerProfileAsync();

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == id && transaction.CustomerId == customerProfile.Id);

        var selectedQuery = query
            .Select(transaction => new Response.GetTransactionItems
            {
                TransactionId = transaction.Id,
                CustomerId = transaction.CustomerId,
                BookingId = transaction.BookingId,
                Amount = transaction.Amount,
                Type = transaction.Type,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            });

        var transaction = await selectedQuery
            .FirstOrDefaultAsync();

        if (transaction == null)
            throw new KeyNotFoundException("Transaction not found.");

        return transaction;
    }
}
