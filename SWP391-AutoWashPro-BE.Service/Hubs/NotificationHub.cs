using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SWP391_AutoWashPro_BE.Service.Hubs;

[Authorize]
public class NotificationHub: Hub
{
    
}

