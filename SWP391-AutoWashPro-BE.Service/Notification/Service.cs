using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Service.Base;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Notification;

public class Service : IService
{
    
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    
    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    
    public async Task<Response.GetNotificationResponse> GetNotification(NotificationType? type, bool? isRead, int page, int pageSize)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        var query = _dbContext.Notifications.Where(x => x.UserId == user.Id);
        if (isRead.HasValue)
        {
            query = query.Where(x => x.IsRead == isRead.Value);
        }

        if (type != null)
        {
            query = query.Where(x => x.Type == type);
        }

        var data = query.Select(x => new Response.NotificationItem
        {
            Id = x.Id,
            Type = x.Type,
            Title = x.Title,
            Content = x.Content,
            IsRead = x.IsRead,
            CreatedAt = x.CreatedAt,
            MetaData = x.Metadata,
        });
        var totalCount = await query.CountAsync();
        var result = new Response.GetNotificationResponse
        {
            Data = data.ToList(),
            UnreadCount = _dbContext.Notifications.Where(x => !x.IsRead).Count(),
            Pagination = new Response.PaginationResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalCount / pageSize,
            }
        };
        return result;
    }

    public async Task<Response.UpdateNotificationStatusResponse> UpdateNotificationStatus(Request.UpdateNotificationStatusRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        
        // Duyet qua tung ID trong mang ID duoc truyen
        var updateCount = 0;
        foreach (var item in request.Ids)
        {
            var unreadMsg = await _dbContext.Notifications.FirstOrDefaultAsync(x =>  x.Id == item );
            if (unreadMsg == null)
            {
                continue;
            }
            updateCount++;
            unreadMsg.IsRead = request.IsRead || unreadMsg.IsRead;
        }
        var unreadCount = await _dbContext.Notifications.Where(x => x.IsRead == false).CountAsync();
        await _dbContext.SaveChangesAsync();
        var result = new Response.UpdateNotificationStatusResponse
        {
            UpdatedCount = updateCount,
            UnreadCount = unreadCount,
        };
        return result;
    }
}