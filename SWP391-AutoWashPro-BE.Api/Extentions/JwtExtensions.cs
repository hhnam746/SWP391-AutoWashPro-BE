using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SWP391_AutoWashPro_BE.Repository.Constants;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.JwtService;

namespace SWP391_AutoWashPro_BE.Api.Extentions;


public static class JwtExtensions
{
    public const string AdminPolicy = "AdminPolicy";
    public const string UserPolicy = "UserPolicy";

    public static void AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        JwtOptions jwtOption = new JwtOptions();
        configuration.GetSection(nameof(JwtOptions)).Bind(jwtOption);
        var key = Encoding.UTF8.GetBytes(jwtOption.SecretKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true, 
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOption.Issuer,
                    ValidAudience = jwtOption.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    //SignalR
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/notificationHub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    
                    OnTokenValidated = async context =>
                    {
                        var userIdRaw = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (string.IsNullOrWhiteSpace(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
                        {
                            context.Fail("Invalid token subject.");
                            return;
                        }

                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var status = await dbContext.Users
                            .AsNoTracking()
                            .Where(x => x.Id == userId)
                            .Select(x => (AccountStatus?)x.Status)
                            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                        if (status is null)
                        {
                            context.Fail("User no longer exists.");
                            return;
                        }

                        // if (status != AccountStatus.Active)
                        // {
                        //     context.Fail("Account is not active.");
                        // }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy =>
                policy.RequireRole(nameof(UserRole.Admin), AppRoles.Admin));
            // [Authorize(Policy = JwtExtensions.AdminPolicy)]
        
            options.AddPolicy(UserPolicy, policy =>
                policy.RequireRole(nameof(UserRole.Customer), AppRoles.Customer));
                // policy.RequireRole("User"));
            // [Authorize(Policy = JwtExtensions.UserPolicy)]
        });
    }
}
