using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Service.Base;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Hubs;

namespace SWP391_AutoWashPro_BE.Service.Notification;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext, IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _hubContext = hubContext;
    }


    public async Task<Response.GetNotificationResponse> GetNotification(NotificationType? type, bool? isRead, int page,
        int pageSize)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new InvalidOperationException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");
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

        data = data
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var result = new Response.GetNotificationResponse
        {
            Data = data.ToList(),
            UnreadCount = await _dbContext.Notifications
                .Where(x => !x.IsRead)
                .CountAsync(),

            Pagination = new Response.PaginationResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            }
        };
        return result;
    }

    public async Task<Response.UpdateNotificationStatusResponse> UpdateNotificationStatus(
        Request.UpdateNotificationStatusRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new InvalidOperationException("User not found");

        // Duyet qua tung ID trong mang ID duoc truyen
        var updateCount = 0;
        foreach (var item in request.Ids)
        {
            var unreadMsg = await _dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == item);
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

    public async Task SendNotification(Request.SendNotificationRequest request)
    {
        var currentUserId = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var currentUser = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == currentUserId);

        if (currentUser == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var receiverUserId = currentUserId;

        if (request.UserId.HasValue)
        {
            // if (currentUser.Role != UserRole.Admin)
            // {
            //     throw new UnauthorizedAccessException("Only admin can send notification to another user.");
            // }

            receiverUserId = request.UserId.Value;
        }

        var (title, content) = NotificationTemplateProvider.GetTemplate(request.Type, request.Data);
        await SendNotificationToUser(
            receiverUserId,
            Guid.NewGuid(),
            request.Type,
            title,
            content,
            request.Metadata);
    }

    public async Task SendNotificationToUser(
        Guid userId,
        Guid notificationId,
        NotificationType type,
        string title,
        string content,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var receiverExists = await _dbContext.Users
            .AnyAsync(x => x.Id == userId, cancellationToken);
        if (!receiverExists)
        {
            throw new InvalidOperationException("Receiver user not found");
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification == null)
        {
            notification = new Repository.Entities.Notification
            {
                Id = notificationId,
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                Metadata = metadata,
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = false
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveNotification", new Response.NotificationItem
            {
                Id = notification.Id,
                Title = notification.Title,
                Content = notification.Content,
                Type = notification.Type,
                IsRead = notification.IsRead,
                MetaData = notification.Metadata ?? string.Empty,
                CreatedAt = notification.CreatedAt
            }, cancellationToken);
    }
}
