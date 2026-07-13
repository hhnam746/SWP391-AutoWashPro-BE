using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.JwtService;
using SWP391_AutoWashPro_BE.Service.MailService;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Service.Auth;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly MediaService.IService _mediaService;
    private readonly MailService.IService _mailService;
    private readonly ILogger<Service> _logger;
    private readonly JwtOptions _jwtOption = new();
    private readonly JwtService.IService _jwtService;
    private readonly Security.IService _securityService;
    private readonly OtpService.IService _otpService;

    public Service(AppDbContext dbContext, MediaService.IService mediaService, MailService.IService mailService,
        ILogger<Service> logger, JwtService.IService jwtService, IConfiguration configuration,
        Security.IService service, OtpService.IService otpService)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
        _mailService = mailService;
        _logger = logger;
        _jwtService = jwtService;
        _securityService = service;
        _otpService = otpService;
        configuration.GetSection(nameof(JwtOptions)).Bind(_jwtOption);
    }


    public async Task<string> Register(Request.RegisterRequest request)
    {
        if (request.FaceImages is null || request.FaceImages.Count < 3)
        {
            throw new ArgumentException("At least 3 face images are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Cccd))
        {
            throw new ArgumentException("CCCD is required.");
        }

        var normalizedEmail = request.Email.Trim();
        var normalizedPhone = request.Phone.Trim();
        var normalizedFirstName = request.FirstName.Trim();
        var normalizedLastName = request.LastName.Trim();
        var normalizedCccd = request.Cccd.Trim();
        var normalizedPassWord = request.Password.Trim();

        if (!IsValidEmail(normalizedEmail))
        {
            throw new ArgumentException("Invalid email format.");
        }

        if (!IsValidPhoneNumber(normalizedPhone))
        {
            throw new ArgumentException("Invalid phone number format.");
        }

        if (!IsValidCccd(normalizedCccd))
        {
            throw new ArgumentException("Invalid CCCD format.");
        }

        if (!IsValidPassword(normalizedPassWord))
        {
            throw new ArgumentException("Password must be at least 8 characters long, " +
                                        "contain at least one uppercase letter," +
                                        " one lowercase letter, one number, and\n  one special character.");
        }

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email == normalizedEmail ||
                x.Phone == normalizedPhone);

        if (existingUser != null)
        {
            if (existingUser.Email == normalizedEmail)
            {
                throw new ArgumentException("Email is already used.");
            }

            if (existingUser.Phone == normalizedPhone)
            {
                throw new ArgumentException("Phone number is already used.");
            }
        }

        var defaultTier = await _dbContext.Tiers
            .OrderBy(t => t.Level)
            .FirstOrDefaultAsync();

        if (defaultTier == null)
        {
            defaultTier = await _dbContext.Tiers
                .OrderBy(t => t.Level)
                .FirstOrDefaultAsync();
        }

        if (defaultTier == null)
        {
            defaultTier = new Repository.Entities.Tier
            {
                Name = "Member",
                Level = 1,
                RequiredWashes = 0,
                PriorityBookingDays = 5,
                Description = "Default tier for newly registered customers.",
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.Tiers.Add(defaultTier);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another concurrent request may have inserted the default tier.
                _dbContext.Entry(defaultTier).State = EntityState.Detached;
                defaultTier = await _dbContext.Tiers
                                  .FirstOrDefaultAsync(t => t.Name == "Member")
                              ?? await _dbContext.Tiers.OrderBy(t => t.Level).FirstOrDefaultAsync();

                if (defaultTier == null)
                {
                    throw;
                }
            }
        }

        // Check duplicate CCCD
        bool isCccdUsed = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .AnyAsync(x => x.Cccd == normalizedCccd);

        if (isCccdUsed)
        {
            throw new ArgumentException("CCCD has already been used.");
        }


        var user = new Repository.Entities.User()
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Phone = normalizedPhone,
            PasswordHash = _securityService.Hash(request.Password),
            isVerify = false,
            Role = UserRole.Customer,
            Status = AccountStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.Users.Add(user);


        var customerProfile = new Repository.Entities.CustomerProfile()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TierId = defaultTier.Id,
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            Cccd = normalizedCccd,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.CustomerProfiles.Add(customerProfile);


        var userWallet = new Repository.Entities.Wallet()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            Balance = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.Wallets.Add(userWallet);


        var uploadTasks = request.FaceImages.Select(img => _mediaService.UploadImageAsync(img));
        var faceImageUrls = await Task.WhenAll(uploadTasks);

        var userFaceImages = faceImageUrls.Select(imageUrl => new Repository.Entities.UserFaceImage()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ImageUrl = imageUrl,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        _dbContext.UserFaceImages.AddRange(userFaceImages);

        await _dbContext.SaveChangesAsync();


        await _mailService.SendMail(new MailContent
        {
            To = request.Email,
            Subject = "Welcome to AutoWash Pro",
             Body = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Welcome to AutoWash Pro</title>
</head>
<body style=""margin:0; padding:0; background-color:#eeeae2; font-family:Arial, Helvetica, sans-serif; color:#111111;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#eeeae2; padding:48px 12px;"">
    <tr>
      <td align=""center"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:680px; background-color:#fffdf9; border-radius:4px; overflow:hidden; box-shadow:0 28px 80px rgba(15,15,15,0.16);"">

          <!-- Luxury Header -->
          <tr>
            <td style=""background-color:#0a0a0b; padding:34px 42px 0; color:#ffffff;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  <td align=""left"" valign=""middle"">
                    <table cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td style=""width:42px; height:42px; border:1px solid #c6a56a; text-align:center; font-family:Georgia, 'Times New Roman', serif; font-size:18px; font-weight:bold; letter-spacing:-1px; color:#e6c98b;"">
                          AW
                        </td>
                        <td style=""padding-left:14px;"">
                          <p style=""margin:0; font-size:15px; font-weight:700; letter-spacing:2.7px; color:#ffffff;"">AUTOWASH</p>
                          <p style=""margin:4px 0 0; font-size:9px; letter-spacing:4px; color:#c6a56a;"">PRO</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                  <td align=""right"" valign=""middle"">
                    <span style=""display:inline-block; border:1px solid #3a352d; padding:9px 13px; font-size:10px; letter-spacing:1.8px; color:#dac18c;"">SILVER MEMBER</span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Editorial Hero -->
          <tr>
            <td style=""background-color:#0a0a0b; padding:52px 42px 62px; text-align:center; color:#ffffff;"">
              <div style=""width:48px; height:1px; background-color:#c6a56a; margin:0 auto 28px;""></div>
              <p style=""margin:0 0 19px; font-size:10px; line-height:1; letter-spacing:4px; color:#c6a56a; font-weight:700;"">
                MEMBERSHIP CONFIRMED
              </p>
              <h1 style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:44px; line-height:1.15; font-weight:normal; letter-spacing:-0.6px; color:#fffdf9;"">
                Welcome to<br />AutoWash Pro
              </h1>
              <p style=""margin:21px auto 0; max-width:440px; font-size:14px; line-height:1.9; letter-spacing:0.2px; color:#b8b5ae;"">
                A refined car care experience built around seamless reservations, attentive service, and rewarding every visit.
              </p>
            </td>
          </tr>

          <!-- Welcome Content -->
          <tr>
            <td style=""padding:44px 42px 0; background-color:#fffdf9;"">
              <p style=""margin:0 0 14px; font-family:Georgia, 'Times New Roman', serif; font-size:24px; line-height:1.45; color:#111111;"">
                Dear {request.FirstName} {request.LastName},
              </p>
              <p style=""margin:0; font-size:14px; line-height:1.9; color:#5a564f;"">
                Your AutoWash Pro membership has been successfully created. From this moment, your visits become more effortless: reserve your preferred wash time, manage your vehicles, and collect loyalty points toward elevated privileges.
              </p>
            </td>
          </tr>

          <!-- Membership Credential -->
          <tr>
            <td style=""padding:33px 42px 0; background-color:#fffdf9;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#111112; border-radius:2px; overflow:hidden;"">
                <tr>
                  <td style=""padding:25px 28px 20px; border-bottom:1px solid #292722;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td>
                          <p style=""margin:0 0 6px; font-size:9px; letter-spacing:3px; color:#c6a56a; font-weight:bold;"">MEMBER PROFILE</p>
                          <p style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:21px; color:#fffdf9;"">Account Details</p>
                        </td>
                        <td align=""right"">
                          <span style=""display:inline-block; padding:8px 13px; border:1px solid #c6a56a; font-size:9px; letter-spacing:2px; color:#e3c581; font-weight:bold;"">ACTIVE</span>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:16px 28px 23px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td style=""padding:12px 0; width:36%; border-bottom:1px solid #292722; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">EMAIL</td>
                        <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:14px; color:#f3efe7;"">{request.Email}</td>
                      </tr>
                      <tr>
                        <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">FULL NAME</td>
                        <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:14px; color:#f3efe7;"">{request.FirstName} {request.LastName}</td>
                      </tr>
                      <tr>
                        <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">ROLE</td>
                        <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:14px; color:#f3efe7;"">Customer</td>
                      </tr>
                      <tr>
                        <td style=""padding:13px 0 0; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">TIER</td>
                        <td style=""padding:13px 0 0; font-size:14px; color:#e3c581; letter-spacing:1px; font-weight:bold;"">SILVER</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Signature Services -->
          <tr>
            <td style=""padding:42px 42px 0; background-color:#fffdf9;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  <td style=""border-bottom:1px solid #dfd8cb; padding-bottom:17px;"">
                    <p style=""margin:0 0 8px; font-size:10px; letter-spacing:3px; color:#9e7c45; font-weight:bold;"">YOUR EXPERIENCE</p>
                    <h2 style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:25px; font-weight:normal; color:#111111;"">Privileges from your first visit</h2>
                  </td>
                </tr>
              </table>

              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin-top:10px;"">
                <tr>
                  <td width=""50%"" valign=""top"" style=""padding:20px 20px 20px 0; border-bottom:1px solid #e6e0d5; border-right:1px solid #e6e0d5;"">
                    <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">01</p>
                    <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Advance Booking</p>
                    <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Reserve your preferred wash slot before arrival.</p>
                  </td>
                  <td width=""50%"" valign=""top"" style=""padding:20px 0 20px 20px; border-bottom:1px solid #e6e0d5;"">
                    <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">02</p>
                    <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Loyalty Points</p>
                    <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Earn points automatically after each completed wash.</p>
                  </td>
                </tr>
                <tr>
                  <td width=""50%"" valign=""top"" style=""padding:20px 20px 0 0; border-right:1px solid #e6e0d5;"">
                    <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">03</p>
                    <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Curated Rewards</p>
                    <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Redeem vouchers, gifts, or complimentary washes.</p>
                  </td>
                  <td width=""50%"" valign=""top"" style=""padding:20px 0 0 20px;"">
                    <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">04</p>
                    <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Elevated Tiers</p>
                    <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Unlock Gold and Platinum booking benefits.</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Next Journey -->
          <tr>
            <td style=""padding:46px 42px 0; background-color:#fffdf9;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f5f1e9; border:1px solid #e2dacb;"">
                <tr>
                  <td style=""padding:30px 29px;"">
                    <p style=""margin:0 0 8px; font-size:10px; letter-spacing:3px; color:#9e7c45; font-weight:bold;"">BEGIN YOUR JOURNEY</p>
                    <h3 style=""margin:0 0 25px; font-family:Georgia, 'Times New Roman', serif; font-size:25px; font-weight:normal; color:#111111;"">Your next steps</h3>
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td style=""width:35px; vertical-align:top; padding:0 0 19px; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#b18a4e;"">I</td>
                        <td style=""padding:0 0 19px; border-bottom:1px solid #dfd5c2; font-size:13px; line-height:1.75; color:#625d54;"">
                          <strong style=""display:block; margin-bottom:3px; font-size:14px; color:#111111;"">Register your vehicle</strong>
                          Add your license plate and vehicle details for a seamless arrival.
                        </td>
                      </tr>
                      <tr>
                        <td style=""width:35px; vertical-align:top; padding:19px 0; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#b18a4e;"">II</td>
                        <td style=""padding:19px 0; border-bottom:1px solid #dfd5c2; font-size:13px; line-height:1.75; color:#625d54;"">
                          <strong style=""display:block; margin-bottom:3px; font-size:14px; color:#111111;"">Reserve a preferred time</strong>
                          Select a branch and available wash slot at your convenience.
                        </td>
                      </tr>
                      <tr>
                        <td style=""width:35px; vertical-align:top; padding:19px 0 0; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#b18a4e;"">III</td>
                        <td style=""padding:19px 0 0; font-size:13px; line-height:1.75; color:#625d54;"">
                          <strong style=""display:block; margin-bottom:3px; font-size:14px; color:#111111;"">Receive your rewards</strong>
                          Earn points with each visit and progress toward exclusive tiers.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Closing -->
          <tr>
            <td style=""padding:40px 42px 46px; background-color:#fffdf9; text-align:center;"">
              <p style=""margin:0 0 4px; font-size:13px; color:#6c665d; line-height:1.8;"">We look forward to serving you.</p>
              <p style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#111111;"">AutoWash Pro Team</p>
            </td>
          </tr>

          <!-- Luxury Footer -->
          <tr>
            <td style=""padding:27px 32px 30px; text-align:center; background-color:#0a0a0b; border-top:2px solid #c6a56a;"">
              <p style=""margin:0 0 13px; font-size:10px; line-height:1.7; letter-spacing:3px; color:#d1ad6b;"">
                AUTOWASH PRO
              </p>
              <p style=""margin:0; font-size:10px; line-height:1.7; letter-spacing:1.8px; color:#77726b;"">
                SMART BOOKING &nbsp;&nbsp;|&nbsp;&nbsp; LOYALTY REWARDS &nbsp;&nbsp;|&nbsp;&nbsp; PREMIUM CARE
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
"
        });
        
        // Send welcome email
//         _ = Task.Run(async () => 
//         {
//             try
//             {
//                 var mailContent = new MailContent()
//                 {
//                     To = request.Email,
//                     Subject = "Welcome to AutoWash Pro",
//                     Body = $@"
// <!DOCTYPE html>
// <html lang=""en"">
// <head>
//   <meta charset=""UTF-8"" />
//   <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
//   <title>Welcome to AutoWash Pro</title>
// </head>
// <body style=""margin:0; padding:0; background-color:#eeeae2; font-family:Arial, Helvetica, sans-serif; color:#111111;"">
//   <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#eeeae2; padding:48px 12px;"">
//     <tr>
//       <td align=""center"">
//         <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:680px; background-color:#fffdf9; border-radius:4px; overflow:hidden; box-shadow:0 28px 80px rgba(15,15,15,0.16);"">
//
//           <!-- Luxury Header -->
//           <tr>
//             <td style=""background-color:#0a0a0b; padding:34px 42px 0; color:#ffffff;"">
//               <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
//                 <tr>
//                   <td align=""left"" valign=""middle"">
//                     <table cellpadding=""0"" cellspacing=""0"" border=""0"">
//                       <tr>
//                         <td style=""width:42px; height:42px; border:1px solid #c6a56a; text-align:center; font-family:Georgia, 'Times New Roman', serif; font-size:18px; font-weight:bold; letter-spacing:-1px; color:#e6c98b;"">
//                           AW
//                         </td>
//                         <td style=""padding-left:14px;"">
//                           <p style=""margin:0; font-size:15px; font-weight:700; letter-spacing:2.7px; color:#ffffff;"">AUTOWASH</p>
//                           <p style=""margin:4px 0 0; font-size:9px; letter-spacing:4px; color:#c6a56a;"">PRO</p>
//                         </td>
//                       </tr>
//                     </table>
//                   </td>
//                   <td align=""right"" valign=""middle"">
//                     <span style=""display:inline-block; border:1px solid #3a352d; padding:9px 13px; font-size:10px; letter-spacing:1.8px; color:#dac18c;"">SILVER MEMBER</span>
//                   </td>
//                 </tr>
//               </table>
//             </td>
//           </tr>
//
//           <!-- Editorial Hero -->
//           <tr>
//             <td style=""background-color:#0a0a0b; padding:52px 42px 62px; text-align:center; color:#ffffff;"">
//               <div style=""width:48px; height:1px; background-color:#c6a56a; margin:0 auto 28px;""></div>
//               <p style=""margin:0 0 19px; font-size:10px; line-height:1; letter-spacing:4px; color:#c6a56a; font-weight:700;"">
//                 MEMBERSHIP CONFIRMED
//               </p>
//               <h1 style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:44px; line-height:1.15; font-weight:normal; letter-spacing:-0.6px; color:#fffdf9;"">
//                 Welcome to<br />AutoWash Pro
//               </h1>
//               <p style=""margin:21px auto 0; max-width:440px; font-size:14px; line-height:1.9; letter-spacing:0.2px; color:#b8b5ae;"">
//                 A refined car care experience built around seamless reservations, attentive service, and rewarding every visit.
//               </p>
//             </td>
//           </tr>
//
//           <!-- Welcome Content -->
//           <tr>
//             <td style=""padding:44px 42px 0; background-color:#fffdf9;"">
//               <p style=""margin:0 0 14px; font-family:Georgia, 'Times New Roman', serif; font-size:24px; line-height:1.45; color:#111111;"">
//                 Dear {request.FirstName} {request.LastName},
//               </p>
//               <p style=""margin:0; font-size:14px; line-height:1.9; color:#5a564f;"">
//                 Your AutoWash Pro membership has been successfully created. From this moment, your visits become more effortless: reserve your preferred wash time, manage your vehicles, and collect loyalty points toward elevated privileges.
//               </p>
//             </td>
//           </tr>
//
//           <!-- Membership Credential -->
//           <tr>
//             <td style=""padding:33px 42px 0; background-color:#fffdf9;"">
//               <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#111112; border-radius:2px; overflow:hidden;"">
//                 <tr>
//                   <td style=""padding:25px 28px 20px; border-bottom:1px solid #292722;"">
//                     <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
//                       <tr>
//                         <td>
//                           <p style=""margin:0 0 6px; font-size:9px; letter-spacing:3px; color:#c6a56a; font-weight:bold;"">MEMBER PROFILE</p>
//                           <p style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:21px; color:#fffdf9;"">Account Details</p>
//                         </td>
//                         <td align=""right"">
//                           <span style=""display:inline-block; padding:8px 13px; border:1px solid #c6a56a; font-size:9px; letter-spacing:2px; color:#e3c581; font-weight:bold;"">ACTIVE</span>
//                         </td>
//                       </tr>
//                     </table>
//                   </td>
//                 </tr>
//                 <tr>
//                   <td style=""padding:16px 28px 23px;"">
//                     <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
//                       <tr>
//                         <td style=""padding:12px 0; width:36%; border-bottom:1px solid #292722; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">EMAIL</td>
//                         <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:14px; color:#f3efe7;"">{request.Email}</td>
//                       </tr>
//                       <tr>
//                         <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">FULL NAME</td>
//                         <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:14px; color:#f3efe7;"">{request.FirstName} {request.LastName}</td>
//                       </tr>
//                       <tr>
//                         <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">ROLE</td>
//                         <td style=""padding:12px 0; border-bottom:1px solid #292722; font-size:14px; color:#f3efe7;"">Customer</td>
//                       </tr>
//                       <tr>
//                         <td style=""padding:13px 0 0; font-size:10px; letter-spacing:1.6px; color:#8c877d;"">TIER</td>
//                         <td style=""padding:13px 0 0; font-size:14px; color:#e3c581; letter-spacing:1px; font-weight:bold;"">SILVER</td>
//                       </tr>
//                     </table>
//                   </td>
//                 </tr>
//               </table>
//             </td>
//           </tr>
//
//           <!-- Signature Services -->
//           <tr>
//             <td style=""padding:42px 42px 0; background-color:#fffdf9;"">
//               <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
//                 <tr>
//                   <td style=""border-bottom:1px solid #dfd8cb; padding-bottom:17px;"">
//                     <p style=""margin:0 0 8px; font-size:10px; letter-spacing:3px; color:#9e7c45; font-weight:bold;"">YOUR EXPERIENCE</p>
//                     <h2 style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:25px; font-weight:normal; color:#111111;"">Privileges from your first visit</h2>
//                   </td>
//                 </tr>
//               </table>
//
//               <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin-top:10px;"">
//                 <tr>
//                   <td width=""50%"" valign=""top"" style=""padding:20px 20px 20px 0; border-bottom:1px solid #e6e0d5; border-right:1px solid #e6e0d5;"">
//                     <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">01</p>
//                     <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Advance Booking</p>
//                     <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Reserve your preferred wash slot before arrival.</p>
//                   </td>
//                   <td width=""50%"" valign=""top"" style=""padding:20px 0 20px 20px; border-bottom:1px solid #e6e0d5;"">
//                     <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">02</p>
//                     <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Loyalty Points</p>
//                     <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Earn points automatically after each completed wash.</p>
//                   </td>
//                 </tr>
//                 <tr>
//                   <td width=""50%"" valign=""top"" style=""padding:20px 20px 0 0; border-right:1px solid #e6e0d5;"">
//                     <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">03</p>
//                     <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Curated Rewards</p>
//                     <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Redeem vouchers, gifts, or complimentary washes.</p>
//                   </td>
//                   <td width=""50%"" valign=""top"" style=""padding:20px 0 0 20px;"">
//                     <p style=""margin:0 0 11px; font-family:Georgia, 'Times New Roman', serif; font-size:23px; color:#c6a56a;"">04</p>
//                     <p style=""margin:0 0 8px; font-size:14px; font-weight:bold; color:#111111;"">Elevated Tiers</p>
//                     <p style=""margin:0; font-size:12px; line-height:1.72; color:#69645b;"">Unlock Gold and Platinum booking benefits.</p>
//                   </td>
//                 </tr>
//               </table>
//             </td>
//           </tr>
//
//           <!-- Next Journey -->
//           <tr>
//             <td style=""padding:46px 42px 0; background-color:#fffdf9;"">
//               <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f5f1e9; border:1px solid #e2dacb;"">
//                 <tr>
//                   <td style=""padding:30px 29px;"">
//                     <p style=""margin:0 0 8px; font-size:10px; letter-spacing:3px; color:#9e7c45; font-weight:bold;"">BEGIN YOUR JOURNEY</p>
//                     <h3 style=""margin:0 0 25px; font-family:Georgia, 'Times New Roman', serif; font-size:25px; font-weight:normal; color:#111111;"">Your next steps</h3>
//                     <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
//                       <tr>
//                         <td style=""width:35px; vertical-align:top; padding:0 0 19px; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#b18a4e;"">I</td>
//                         <td style=""padding:0 0 19px; border-bottom:1px solid #dfd5c2; font-size:13px; line-height:1.75; color:#625d54;"">
//                           <strong style=""display:block; margin-bottom:3px; font-size:14px; color:#111111;"">Register your vehicle</strong>
//                           Add your license plate and vehicle details for a seamless arrival.
//                         </td>
//                       </tr>
//                       <tr>
//                         <td style=""width:35px; vertical-align:top; padding:19px 0; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#b18a4e;"">II</td>
//                         <td style=""padding:19px 0; border-bottom:1px solid #dfd5c2; font-size:13px; line-height:1.75; color:#625d54;"">
//                           <strong style=""display:block; margin-bottom:3px; font-size:14px; color:#111111;"">Reserve a preferred time</strong>
//                           Select a branch and available wash slot at your convenience.
//                         </td>
//                       </tr>
//                       <tr>
//                         <td style=""width:35px; vertical-align:top; padding:19px 0 0; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#b18a4e;"">III</td>
//                         <td style=""padding:19px 0 0; font-size:13px; line-height:1.75; color:#625d54;"">
//                           <strong style=""display:block; margin-bottom:3px; font-size:14px; color:#111111;"">Receive your rewards</strong>
//                           Earn points with each visit and progress toward exclusive tiers.
//                         </td>
//                       </tr>
//                     </table>
//                   </td>
//                 </tr>
//               </table>
//             </td>
//           </tr>
//
//           <!-- Closing -->
//           <tr>
//             <td style=""padding:40px 42px 46px; background-color:#fffdf9; text-align:center;"">
//               <p style=""margin:0 0 4px; font-size:13px; color:#6c665d; line-height:1.8;"">We look forward to serving you.</p>
//               <p style=""margin:0; font-family:Georgia, 'Times New Roman', serif; font-size:20px; color:#111111;"">AutoWash Pro Team</p>
//             </td>
//           </tr>
//
//           <!-- Luxury Footer -->
//           <tr>
//             <td style=""padding:27px 32px 30px; text-align:center; background-color:#0a0a0b; border-top:2px solid #c6a56a;"">
//               <p style=""margin:0 0 13px; font-size:10px; line-height:1.7; letter-spacing:3px; color:#d1ad6b;"">
//                 AUTOWASH PRO
//               </p>
//               <p style=""margin:0; font-size:10px; line-height:1.7; letter-spacing:1.8px; color:#77726b;"">
//                 SMART BOOKING &nbsp;&nbsp;|&nbsp;&nbsp; LOYALTY REWARDS &nbsp;&nbsp;|&nbsp;&nbsp; PREMIUM CARE
//               </p>
//             </td>
//           </tr>
//
//         </table>
//       </td>
//     </tr>
//   </table>
// </body>
// </html>
// "
//                 };
//                 await _mailService.SendMail(mailContent);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to send welcome email to {Email}", request.Email);
//             }
//         });

        return "User registered successfully!";
    }


    public async Task<Response.LoginResponse> Login(Request.LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Identifier || u.Phone == request.Identifier);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var isPasswordValid = _securityService.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status == AccountStatus.Locked)
        {
            throw new ForbiddenAccessException("Account is locked");
        }

        if (user.Status == AccountStatus.Inactive)
        {
            throw new ForbiddenAccessException("Account is inactive");
        }

        // if (user.Status != AccountStatus.Active)
        // {
        //     throw new ForbiddenAccessException("Account is not active");
        // }

        //account pending, reject được thông qua

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("Role", user.Role.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.Expired,
                DateTimeOffset.UtcNow.AddMinutes(_jwtOption.ExpireMinutes).ToString()),
        };

        var accessToken = _jwtService.GenerateAccessToken(claims);

        return new Response.LoginResponse
        {
            Access_token = accessToken,
            isVerify = user.isVerify
        };
    }

    public async Task ForgotPassword(Request.ForgotPasswordRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            throw new ArgumentException("Email does not exist.");
        }

        if (user.Status == AccountStatus.Locked)
        {
            throw new ForbiddenAccessException("Account is locked");
        }

        if (user.Status == AccountStatus.Inactive)
        {
            throw new ForbiddenAccessException("Account is inactive");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User email is not available.");
        }

        await _otpService.GenerateAndSendOtpAsync(user.Email);
    }

    public async Task<Response.ResetPasswordResponse> VerifyForgotPassword(Request.VerifyOtpRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            throw new ArgumentException("Email does not exist.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("User email is not available.");
        }

        var isOtpValid = await _otpService.VerifyOtpAsync(user.Email, request.Otp);
        if (!isOtpValid)
        {
            throw new ArgumentException("Invalid or expired OTP.");
        }

        // Tạo một JWT token ngắn hạn (vd 15 phút) để đánh dấu đã xác thực OTP thành công
        // FE sẽ dùng token này để gọi api ResetPassword
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("Purpose", "ResetPassword"),
            new Claim(ClaimTypes.Expired, DateTimeOffset.UtcNow.AddMinutes(15).ToString()),
        };

        var resetPasswordToken = _jwtService.GenerateAccessToken(claims);

        return new Response.ResetPasswordResponse
        {
            ResetPasswordToken = resetPasswordToken
        };
    }

    public async Task ResetPassword(Request.ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("Confirm password does not match.");
        }

        if (!IsValidPassword(request.NewPassword))
        {
            throw new ArgumentException("Password must be at least 8 characters long, " +
                                        "contain at least one uppercase letter," +
                                        " one lowercase letter, one number, and one special character.");
        }

        // Validate cái ResetPasswordToken lấy được ở bước trước
        var principal = _jwtService.ValidateToken(request.ResetPasswordToken);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset password token.");
        }

        var purposeClaim = principal.FindFirst("Purpose")?.Value;
        if (purposeClaim != "ResetPassword")
        {
            throw new UnauthorizedAccessException("Invalid token purpose.");
        }

        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            throw new UnauthorizedAccessException("Invalid token subject.");
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        user.PasswordHash = _securityService.Hash(request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            // Regex pattern for email validation
            // Pattern: [anything]@[anything].[anything]
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        try
        {
            // Regex pattern for phone number validation
            // Pattern: 10-15 digits, can include +, -, space, ()
            string pattern = @"^[0-9+\-\s()]{10,15}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidCccd(string cccd)
    {
        try
        {
            // Vietnam CCCD is commonly stored as exactly 12 digits.
            const string pattern = @"^\d{12}$";
            return Regex.IsMatch(cccd, pattern);
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPassword(string password)
    {
        try
        {
            // Regex giải thích:
            // ^ = bắt đầu chuỗi
            // (?=.*[a-z]) = chứa ít nhất 1 chữ thường
            // (?=.*[A-Z]) = chứa ít nhất 1 chữ hoa
            // (?=.*\d) = chứa ít nhất 1 số
            // (?=.*[@$!%*?&]) = chứa ít nhất 1 ký tự đặc biệt
            // [A-Za-z\d@$!%*?&]{8,} = độ dài tối thiểu 8 ký tự, chỉ gồm các ký tự này
            // $ = kết thúc chuỗi
            const string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            return Regex.IsMatch(password, pattern);
        }
        catch
        {
            return false;
        }
    }
}
