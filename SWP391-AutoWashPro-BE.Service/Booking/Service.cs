using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Booking;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Notification.IService _notificationService;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext, IServiceScopeFactory serviceScopeFactory,
        Notification.IService notificationService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _serviceScopeFactory = serviceScopeFactory;
        _notificationService = notificationService;
    }


    private static readonly TimeSpan DefaultUtcOffset = TimeSpan.FromHours(7);
    private int WorkingStartHour = 0;
    private int WorkingEndHour = 0;
    private int SlotDurationMinutes = 0;

    public async Task<Response.GetBookingSlotsResponse> GetBookingSlots(Guid branchId, DateOnly date)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");

        var bookings = await _dbContext.Bookings
            .Where(x =>
                x.BranchId == branchId &&
                x.BookingDate == date &&
                x.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var workingStartHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingStartHour");

        if (workingStartHourConfig == null)
        {
            throw new Exception("WorkingStartHour config not found");
        }

        if (!int.TryParse(workingStartHourConfig.ConfigValue, out var workingStartHour))
        {
            throw new Exception("Invalid WorkingStartHour config value");
        }

        ///////////////// ///////////////// ///////////////// /////////////////
        WorkingStartHour = workingStartHour;
        ///////////////// ///////////////// ///////////////// /////////////////

        var workingEndHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingEndHour");

        if (workingEndHourConfig == null)
        {
            throw new Exception("WorkingEndHour config not found");
        }

        if (!int.TryParse(workingEndHourConfig.ConfigValue, out var workingEndHour))
        {
            throw new Exception("Invalid WorkingEndHour config value");
        }

        ///////////////// ///////////////// ///////////////// /////////////////
        WorkingEndHour = workingEndHour;
        ///////////////// ///////////////// ///////////////// /////////////////

        var slotDurationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "SlotDurationMinutes")
                                 ?? throw new Exception("SlotDurationMinutes config not found");

        if (!int.TryParse(slotDurationConfig.ConfigValue, out SlotDurationMinutes))
        {
            throw new Exception("Invalid SlotDurationMinutes config value");
        }

        var slots = new List<Response.SlotStatus>();

        var currentTime = new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            WorkingStartHour,
            0,
            0,
            TimeSpan.FromHours(7));

        var endWorkTime = new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            WorkingEndHour,
            0,
            0,
            TimeSpan.FromHours(7));
        ;

        while (currentTime.AddMinutes(SlotDurationMinutes) <= endWorkTime)
        {
            var slotEndTime = currentTime.AddMinutes(SlotDurationMinutes);

            var bookedSlot = bookings.FirstOrDefault(x =>
                x.StartTime.UtcDateTime == currentTime.UtcDateTime &&
                x.EndTime.UtcDateTime == slotEndTime.UtcDateTime);

            slots.Add(new Response.SlotStatus
            {
                StartTime = currentTime,
                EndTime = slotEndTime,
                Status = bookedSlot != null ? bookedSlot.Status : BookingStatus.Available
            });

            currentTime = slotEndTime;
        }

        var result = new Response.GetBookingSlotsResponse
        {
            BranchId = branchId,
            Date = date,
            SlotDurationMinutes = SlotDurationMinutes,
            Data = slots
        };

        return result;
    }

    public async Task<Response.CreateBookingResponse> CreateBooking(Request.CreateBookingRequest bookingRequest)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        // var hasActiveBooking = await _dbContext.Bookings.AnyAsync(x =>
        //     x.VehicleId == bookingRequest.VehicleId &&
        //     x.Status != BookingStatus.Completed &&
        //     x.Status != BookingStatus.Cancelled);
        // if (hasActiveBooking)
        // {
        //     throw new Exception("Vehicle already has active booking");
        // }
        var workingStartHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingStartHour");

        if (workingStartHourConfig == null)
        {
            throw new Exception("WorkingStartHour config not found");
        }

        if (!int.TryParse(workingStartHourConfig.ConfigValue, out var workingStartHour))
        {
            throw new Exception("Invalid WorkingStartHour config value");
        }

        ///////////////// ///////////////// ///////////////// /////////////////
        WorkingStartHour = workingStartHour;
        ///////////////// ///////////////// ///////////////// /////////////////

        var workingEndHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingEndHour");

        if (workingEndHourConfig == null)
        {
            throw new Exception("WorkingEndHour config not found");
        }

        if (!int.TryParse(workingEndHourConfig.ConfigValue, out var workingEndHour))
        {
            throw new Exception("Invalid WorkingEndHour config value");
        }

        ///////////////// ///////////////// ///////////////// /////////////////
        WorkingEndHour = workingEndHour;
        ///////////////// ///////////////// ///////////////// /////////////////

        /////////////// Base Price Config /////////////
        var basePriceConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "BasePrice");

        if (basePriceConfig == null)
        {
            throw new Exception("BasePrice config not found");
        }

        if (!decimal.TryParse(basePriceConfig.ConfigValue, out var basePrice))
        {
            throw new Exception("Invalid BasePrice config value");
        }

        ///////////////////////////////// Sedan /////////////////////////////////////////////// 
        var SedanBaseConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "SedanBasePrice");


        if (SedanBaseConfig == null)
        {
            throw new Exception("SedanBasePrice config not found");
        }

        if (!decimal.TryParse(SedanBaseConfig.ConfigValue, out var SedanBasePrice))
        {
            throw new Exception("Invalid SedanBasePrice config value");
        }


        ///////////////////////////////// SUV ///////////////////////////////////////////////
        var SuvBaseConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "SuvBasePrice");

        if (SuvBaseConfig == null)
        {
            throw new Exception("SuvBasePrice config not found");
        }

        if (!decimal.TryParse(SuvBaseConfig.ConfigValue, out var SuvBasePrice))
        {
            throw new Exception("Invalid SuvBasePrice config value");
        }

        var vehicle = await _dbContext.Vehicles
            .Include(x => x.VehicleType)
            .FirstOrDefaultAsync(x => x.Id == bookingRequest.VehicleId);
        if (vehicle == null)
        {
            throw new Exception("Vehicle not found");
        }

        if (vehicle.VehicleType.TypeName == VehicleTypes.Sedan)
        {
            basePrice += SedanBasePrice;
        }
        else if (vehicle.VehicleType.TypeName == VehicleTypes.SUV)
        {
            basePrice += SuvBasePrice;
        }

        if (bookingRequest.StartTime <= DateTimeOffset.Now)
        {
            throw new Exception("Cannot book past time");
        }

        var bookingLocalDate = DateOnly.FromDateTime(bookingRequest.StartTime);
        if (bookingLocalDate != bookingRequest.BookingDate)
        {
            throw new Exception("BookingDate must match StartTime date.");
        }

        var slotDurationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "SlotDurationMinutes")
                                 ?? throw new Exception("SlotDurationMinutes config not found");

        if (!int.TryParse(slotDurationConfig.ConfigValue, out SlotDurationMinutes))
        {
            throw new Exception("Invalid SlotDurationMinutes config value");
        }

        if (bookingRequest.StartTime.Minute % SlotDurationMinutes != 0 ||
            bookingRequest.StartTime.Second != 0 ||
            bookingRequest.StartTime.Millisecond != 0)
        {
            throw new Exception($"StartTime must align to {SlotDurationMinutes}-minute slot boundaries.");
        }

        var localStartTimeOnly = TimeOnly.FromDateTime(bookingRequest.StartTime);
        var workingStart = new TimeOnly(WorkingStartHour, 0);
        var workingEnd = new TimeOnly(WorkingEndHour, 0);

        if (localStartTimeOnly < workingStart || localStartTimeOnly >= workingEnd)
        {
            throw new Exception($"StartTime must be within working hours ({workingStart}-{workingEnd}).");
        }

        var utcStartTime = bookingRequest.StartTime.ToUniversalTime();

        var isBooked = _dbContext.Bookings.Any(x =>
            x.BranchId == bookingRequest.BranchId && x.BookingDate == bookingRequest.BookingDate
                                                  && x.StartTime == utcStartTime
                                                  && x.Status != BookingStatus.Cancelled);
        if (isBooked)
        {
            throw new Exception("Slot already booked");
        }

        var canBooked = bookingRequest.StartTime - DateTimeOffset.Now;
        if ((int)canBooked.TotalDays > customerProfile.Tier.PriorityBookingDays)
        {
            throw new Exception("Your rank is not enough for this booked");
        }

        //Discount Voucher
        var voucherDiscountAmount = (decimal)0;
        var voucher = await _dbContext.Vouchers
            .FirstOrDefaultAsync(x => x.Id == bookingRequest.VoucherId);

        if (voucher != null)
        {
            if (voucher.DiscountType == DiscountType.FixedAmount)
            {
                voucherDiscountAmount += voucher.DiscountValue;
            }
            else
            {
                voucherDiscountAmount += (basePrice * voucher.DiscountValue) / 100;
            }
        }

        //Discount by promotion
        decimal promotionDiscountAmount = 0;

        // Global Promotions
        var globalPromotions = await _dbContext.Promotions
            .Where(x => x.IsGlobal == true)
            .ToListAsync();

        foreach (var promotion in globalPromotions)
        {
            if (promotion.DiscountType == DiscountType.Percentage)
            {
                promotionDiscountAmount +=
                    basePrice * promotion.DiscountValue / 100;
            }
            else
            {
                promotionDiscountAmount +=
                    promotion.DiscountValue;
            }
        }

        // Tier Promotion
        var tierPromotion = await _dbContext.PromotionTiers
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync(x => x.TierId == customerProfile.TierId);

        if (tierPromotion != null)
        {
            var promotion = tierPromotion.Promotion;

            if (promotion.DiscountType == DiscountType.Percentage)
            {
                promotionDiscountAmount +=
                    basePrice * promotion.DiscountValue / 100;
            }
            else
            {
                promotionDiscountAmount +=
                    promotion.DiscountValue;
            }
        }

        var discountAmount = voucherDiscountAmount + promotionDiscountAmount;

        // Discount By Redeem
        decimal redeemDiscountAmount = 0;

        if (bookingRequest.redemPoint == true)
        {
            var remainToDiscount = basePrice - discountAmount;

            if (customerProfile.TotalPoints >= remainToDiscount)
            {
                redeemDiscountAmount = remainToDiscount;
            }
            else
            {
                redeemDiscountAmount = customerProfile.TotalPoints;
            }
        }

        discountAmount += redeemDiscountAmount;

        if (customerProfile.TotalWashes > 0 && customerProfile.TotalWashes % 5 == 0)
        {
            discountAmount = basePrice;
        }

        var utcEndTime = utcStartTime.AddMinutes(SlotDurationMinutes);

        //Wallet
        if (discountAmount > basePrice)
        {
            discountAmount = basePrice;
        }

        var finalPrice = basePrice - discountAmount;
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        if (wallet == null)
        {
            throw new Exception("Wallet not exists");
        }

        var paymentDepositeConfig = await _dbContext.SystemConfigs
                                        .FirstOrDefaultAsync(x => x.ConfigKey == "PaymentDeposite")
                                    ?? throw new Exception("PaymentDeposite config not found");

        if (!decimal.TryParse(paymentDepositeConfig.ConfigValue, out var paymentDeposite))
        {
            throw new Exception("Invalid PaymentDeposite config value");
        }

        if (wallet.Balance - finalPrice * (paymentDeposite / 100) < 0)
        {
            throw new Exception("Not enough balance");
        }

        wallet.Balance -= finalPrice * (paymentDeposite / 100);
        var newId = Guid.NewGuid();
        var newBooking = new Repository.Entities.Booking()
        {
            Id = newId,
            CustomerId = customerProfile.Id,
            BranchId = bookingRequest.BranchId,
            VehicleId = bookingRequest.VehicleId,
            VoucherId = bookingRequest.VoucherId,
            BookingDate = bookingRequest.BookingDate,
            StartTime = utcStartTime,
            EndTime = utcEndTime,
            Status = BookingStatus.Confirmed,
            BasePrice = basePrice,
            DiscountAmount = discountAmount,
            RedemAmount = (int)redeemDiscountAmount,
            FinalPrice = finalPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Bookings.Add(newBooking);
        var DepositTransaction = new Repository.Entities.Transaction
        {
            Amount = finalPrice * (paymentDeposite / 100),
            Type = Repository.Enums.TransactionType.Deposit,
            Description = "Deposite Booking",
            TransactionDate = DateTime.UtcNow,
            CustomerId = customerProfile.Id,
            CustomerProfile = customerProfile,
            BookingId = newId,
            Booking = newBooking,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Transactions.Add(DepositTransaction);
        var Branch = _dbContext.Branches.FirstOrDefault(x => x.Id == bookingRequest.BranchId);
        var notification = new Repository.Entities.Notification()
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            Type = NotificationType.BookingCreated,
            Title = "Booking Confirmed",
            Content =
                $"Your booking at {Branch.Name} has been confirmed for {bookingRequest.StartTime:HH:mm dd/MM/yyyy}.",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Notifications.Add(notification);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new Exception("Slot already booked");
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var reminderTime = utcStartTime.AddDays(-1);

                var delayTime = reminderTime - DateTimeOffset.UtcNow;

                if (delayTime > TimeSpan.Zero)
                {
                    await Task.Delay(delayTime);

                    using var scope = _serviceScopeFactory.CreateScope();

                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var reminderBooking = await dbContext.Bookings
                        .FirstOrDefaultAsync(x => x.Id == newBooking.Id);

                    if (reminderBooking != null &&
                        reminderBooking.Status == BookingStatus.Confirmed)
                    {
                        var branch = await dbContext.Branches
                            .FirstOrDefaultAsync(x => x.Id == reminderBooking.BranchId);

                        var customer = await dbContext.CustomerProfiles
                            .FirstOrDefaultAsync(x => x.Id == reminderBooking.CustomerId);

                        if (customer != null)
                        {
                            var reminderNotification = new Repository.Entities.Notification()
                            {
                                Id = Guid.NewGuid(),
                                UserId = customer.UserId,
                                Type = NotificationType.BookingReminder,
                                Title = "Booking Reminder",
                                Content =
                                    $"Reminder: Your booking at {branch?.Name ?? "our branch"} starts at {reminderBooking.StartTime.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}.",
                                IsRead = false,
                                CreatedAt = DateTimeOffset.UtcNow,
                            };

                            dbContext.Notifications.Add(reminderNotification);
                            // await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
                            // {
                            //     UserId = userIdGuid,
                            //     Type = NotificationType.BookingCreated,
                            //     Data = $"Booking Success! Your booking at {branch?.Name ?? "our branch"} starts at {reminderBooking.StartTime.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}.",
                            // });
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("======= Reminder Error " + e.Message + " ==========");
            }
        });
        ///////////////////////////////// Thên cronjob cancel tự động
        // ================================= Auto Cancel Cronjob =================================
        _ = Task.Run(async () =>
        {
            try
            {
                // Thời điểm auto cancel = StartTime + 1 phút
                var autoCancelTime = utcStartTime.AddMinutes(1);
                var delayTime = autoCancelTime - DateTimeOffset.UtcNow;

                if (delayTime > TimeSpan.Zero)
                {
                    await Task.Delay(delayTime);
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await dbContext.Bookings
                    .FirstOrDefaultAsync(x => x.Id == newId);

                // Chỉ cancel nếu vẫn còn Confirmed (chưa CheckIn, chưa Cancel,...)
                if (booking != null && booking.Status == BookingStatus.Confirmed)
                {
                    booking.Status = BookingStatus.Cancelled;

                    // // Hoàn tiền deposit về wallet
                    // var customerWallet = await dbContext.Wallets
                    //     .FirstOrDefaultAsync(x => x.CustomerId == booking.CustomerId);
                    //
                    // if (customerWallet != null)
                    // {
                    //     var depositRefund = booking.FinalPrice * (paymentDeposite / 100);
                    //     customerWallet.Balance += depositRefund;
                    // }

                    // Gửi notification cho customer
                    var customer = await dbContext.CustomerProfiles
                        .FirstOrDefaultAsync(x => x.Id == booking.CustomerId);

                    if (customer != null)
                    {
                        var branch = await dbContext.Branches
                            .FirstOrDefaultAsync(x => x.Id == booking.BranchId);

                        var cancelNotification = new Repository.Entities.Notification()
                        {
                            Id = Guid.NewGuid(),
                            UserId = customer.UserId,
                            Type = NotificationType.BookingCancelled,
                            Title = "Booking Auto-Cancelled",
                            Content = $"Your booking at {branch?.Name ?? "our branch"} on " +
                                      $"{booking.StartTime.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy} " +
                                      $"has been automatically cancelled due to no check-in.",
                            IsRead = false,
                            CreatedAt = DateTimeOffset.UtcNow,
                        };
                        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
                        {
                            UserId = userIdGuid,
                            Type = NotificationType.BookingCancelled,
                            Data = $"Your booking at {branch?.Name ?? "our branch"} " +
                                   $"for {booking.StartTime:HH:mm dd/MM/yyyy} " +
                                   $"has been cancelled due to over check-in time.",
                        });

                        dbContext.Notifications.Add(cancelNotification);
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("======= Auto Cancel Error " + e.Message + " ==========");
            }
        });
        // ================================= End Auto Cancel Cronjob =================================
        var result = new Response.CreateBookingResponse
        {
            Id = newId,
            Status = newBooking.Status,
            Branch = new Response.BookingBranch
            {
                Id = newBooking.BranchId,
                Name = _dbContext.Branches.FirstOrDefault(x => x.Id == newBooking.BranchId).Name,
            },
            Vehicle = new Response.BookingVehicle
            {
                Id = newBooking.VehicleId,
                LicensePlate = _dbContext.Vehicles.FirstOrDefault(x => x.Id == newBooking.VehicleId).LicensePlate,
            },
            BookingDate = newBooking.BookingDate,
            StartTime = newBooking.StartTime.ToOffset(TimeSpan.FromHours(7)),
            EndTime = newBooking.EndTime.ToOffset(TimeSpan.FromHours(7)),
            BasePrice = basePrice,
            DiscountAmount = discountAmount,
            FinalPrice = finalPrice,
        };

        return result;
    }

    public async Task<Response.GetBookingsResponse> GetBookings(BookingStatus? status, DateOnly fromDate,
        DateOnly toDate, int page, int pageSize)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        var query = _dbContext.Bookings.Where(x => x.CustomerId == customerProfile.Id);
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status);
        }

        if (fromDate != null)
        {
            query = query.Where(x => x.BookingDate >= fromDate);
        }

        if (toDate != null)
        {
            query = query.Where(x => x.BookingDate <= toDate);
        }

        var bookingDetail = query.Select(x => new Response.BookingItem
        {
            Id = x.Id,
            Status = x.Status,
            BookingDate = x.BookingDate,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            Branch = new Response.BookingBranchDetail
            {
                Id = x.BranchId,
                Name = x.Branch.Name,
                Address = x.Branch.Address,
            },
            Vehicle = new Response.BookingVehicle
            {
                Id = x.VehicleId,
                LicensePlate = x.Vehicle.LicensePlate,
            },
            FinalPrice = x.FinalPrice
        });
        var totalCount = await query.CountAsync();
        bookingDetail = bookingDetail
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var result = new Response.GetBookingsResponse
        {
            Data = await bookingDetail.ToListAsync(),
            Pagination = new Response.Pagination
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        };

        return result;
    }

    public async Task<Response.GetBookingDetailResponse> GetBookingById(Guid bookingId)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");
        var query = await _dbContext.Bookings
            .Include(x => x.Branch)
            .Include(x => x.Vehicle)
            .Include(x => x.Voucher)
            .FirstOrDefaultAsync(x =>
                x.CustomerId == customerProfile.Id &&
                x.Id == bookingId);
        if (query == null)
        {
            throw new Exception("Booking not found");
        }

        var Voucher = query.Voucher == null
            ? null
            : new Response.VoucherInfo()
            {
                Id = query.VoucherId,
                Code = query.Voucher.Code,
                DiscountAmount = query.Voucher.DiscountValue
            };
        var discountAmount = Voucher?.DiscountAmount ?? 0;
        var result = new Response.GetBookingDetailResponse
        {
            Id = query.Id,
            Status = query.Status,
            BookingDate = query.BookingDate,
            StartTime = query.StartTime.ToOffset(TimeSpan.FromHours(7)),
            EndTime = query.EndTime.ToOffset(TimeSpan.FromHours(7)),
            Branch = new Response.BookingBranchDetail
            {
                Id = query.BranchId,
                Name = query.Branch.Name,
                Address = query.Branch.Address,
            },
            Vehicle = new Response.BookingVehicleDetail
            {
                Id = query.VehicleId,
                LicensePlate = query.Vehicle.LicensePlate,
                Brand = query.Vehicle.Brand,
                Model = query.Vehicle.Model,
            },
            Voucher = Voucher,
            BasePrice = query.BasePrice,
            DiscountAmount = discountAmount,
            FinalPrice = query.FinalPrice,
        };
        return result;
    }

    public async Task<Response.CheckInBookingResponse> CheckInBooking(Guid Id)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");

        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        var booking =
            await _dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == Id && x.Status == BookingStatus.Confirmed);
        if (booking == null)
        {
            throw new Exception("Booking not found or has been check-in");
        }

        var slotDurationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "SlotDurationMinutes")
                                 ?? throw new Exception("SlotDurationMinutes config not found");

        if (!int.TryParse(slotDurationConfig.ConfigValue, out SlotDurationMinutes))
        {
            throw new Exception("Invalid SlotDurationMinutes config value");
        }

        var msg = "";
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        var voucher = await _dbContext.Vouchers.FirstOrDefaultAsync(x => x.Id == booking.VoucherId);
        if (wallet == null)
        {
            throw new Exception("Wallet not found");
        }

        var paymentDepositeConfig = await _dbContext.SystemConfigs
                                        .FirstOrDefaultAsync(x => x.ConfigKey == "PaymentDeposite")
                                    ?? throw new Exception("PaymentDeposite config not found");

        if (!decimal.TryParse(paymentDepositeConfig.ConfigValue, out var paymentDeposite))
        {
            throw new Exception("Invalid PaymentDeposite config value");
        }

        if (wallet.Balance - (booking.FinalPrice - (booking.FinalPrice * (paymentDeposite / 100))) < 0)
        {
            booking.Status = BookingStatus.Cancelled;
            msg = "Check-in Failed";
        }
        else
        {
            var remainingAmount = booking.FinalPrice - (booking.FinalPrice * (paymentDeposite / 100));
            wallet.Balance -= remainingAmount;
            booking.Status = BookingStatus.InProgress;
            msg = "Check-in successful";
            if (voucher != null)
            {
                voucher.Status = VoucherStatus.Used;
            }

            var FullPayemntBookingTransaction = new Repository.Entities.Transaction
            {
                Amount = remainingAmount,
                Type = Repository.Enums.TransactionType.FullPayment,
                Description = "Full Payment for Booking",
                TransactionDate = DateTime.UtcNow,
                CustomerId = customerProfile.Id,
                CustomerProfile = customerProfile,
                BookingId = booking.Id,
                Booking = booking,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Transactions.Add(FullPayemntBookingTransaction);
            //////////////////////////////////////////////////////
            customerProfile.TotalPoints -= booking.RedemAmount ?? 0;
            //////////////////////////////////////////////////////

            var bonusPointConfig = await _dbContext.SystemConfigs
                                       .FirstOrDefaultAsync(x => x.ConfigKey == "BonusPoint")
                                   ?? throw new Exception("BonusPoint config not found");

            if (!int.TryParse(bonusPointConfig.ConfigValue, out var bonusPoint))
            {
                throw new Exception("Invalid BonusPoint config value");
            }

            customerProfile.TotalWashes += 1;
            customerProfile.TotalPoints += bonusPoint;
            var currentTier = _dbContext.Tiers.FirstOrDefault(x => x.Level == customerProfile.Tier.Level);
            var nextTier = _dbContext.Tiers.FirstOrDefault(x => x.Level == customerProfile.Tier.Level + 1);

            if (nextTier != null &&
                customerProfile.TotalWashes >= nextTier.RequiredWashes)
            {
                customerProfile.TierId = nextTier.Id;
                customerProfile.Tier = nextTier;
                var notification = new Repository.Entities.Notification()
                {
                    Id = Guid.NewGuid(),
                    UserId = userIdGuid,
                    Type = NotificationType.TierUpgraded,
                    Title = "Tier Upgraded",
                    Content =
                        $"Congratulations! Your tier has been upgraded from {currentTier.Name} to {nextTier.Name}.",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
                {
                    UserId = userIdGuid,
                    Type = NotificationType.TierUpgraded,
                    Data = $"Congratulations! Your tier has been upgraded from {currentTier.Name} to {nextTier.Name}."
                });
                _dbContext.Notifications.Add(notification);
            }

            if (booking.RedemAmount != null)
            {
                var pointTransaction = new Repository.Entities.PointTransaction()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerProfile.Id,
                    Customer = customerProfile,
                    Booking = booking,
                    BookingId = booking.Id,
                    Points = booking.RedemAmount ?? 0,
                    TransactionType = PointTransactionType.Redeem,
                    Description = $"Redeemed {booking.RedemAmount} points for booking payment.",
                    CreatedAt = DateTime.UtcNow,
                };
                _dbContext.PointTransactions.Add(pointTransaction);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(SlotDurationMinutes));
                    using var scope = _serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var delayedBooking = await dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == booking.Id);
                    if (delayedBooking != null && delayedBooking.Status == BookingStatus.InProgress)
                    {
                        delayedBooking.Status = BookingStatus.Completed;
                        delayedBooking.CompletedAt = DateTime.UtcNow;
                        var branch = dbContext.Branches.FirstOrDefault(x => x.Id == delayedBooking.BranchId);
                        var customer =
                            dbContext.CustomerProfiles.FirstOrDefault(x => x.Id == delayedBooking.CustomerId);
                        if (customer != null)
                        {
                            var notification = new Repository.Entities.Notification()
                            {
                                Id = Guid.NewGuid(),
                                UserId = customer.UserId,
                                Type = NotificationType.BookingCompleted,
                                Title = "Booking Completed",
                                Content =
                                    $"Your booking at {branch?.Name ?? "our branch"} has been completed successfully. Thank you for using our service.",
                                IsRead = false,
                                CreatedAt = DateTimeOffset.UtcNow,
                            };
                            dbContext.Notifications.Add(notification);
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("=======Error Occure " + e.Message + " ==========");
                }
            });
        }

        await _dbContext.SaveChangesAsync();

        var result = new Response.CheckInBookingResponse
        {
            Id = booking.Id,
            Status = booking.Status,
            CheckedInAt = booking.StartTime,
            EstimatedCompletedAt = booking.EndTime,
            Message = msg
        };
        return result;
    }

    public async Task<Response.CancelBookingResponse> CancelBooking(
        Guid Id,
        Request.CancelBookingRequest bookingRequest)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(x => x.Id == Id);

        if (booking == null)
        {
            throw new Exception("Booking not found");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new Exception("Booking has already been cancelled");
        }

        if (booking.Status == BookingStatus.InProgress ||
            booking.Status == BookingStatus.Completed)
        {
            throw new Exception("This booking cannot be cancelled");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new Exception("Only confirmed bookings can be cancelled");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        if ((booking.BookingDate.DayNumber - today.DayNumber) < 1)
        {
            throw new Exception("Booking is too close to the scheduled time to cancel");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == booking.BranchId);

        var notification = new Repository.Entities.Notification()
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            Type = NotificationType.BookingCancelled,
            Title = "Booking Cancelled",
            Content =
                $"Your booking at {branch?.Name ?? "our branch"} " +
                $"for {booking.StartTime:HH:mm dd/MM/yyyy} " +
                $"has been cancelled successfully.",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        // await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
        // {
        //     UserId = userIdGuid,
        //     Type = NotificationType.BookingCancelled,
        //     Data = $"Your booking at {branch?.Name ?? "our branch"} " +
        //            $"for {booking.StartTime:HH:mm dd/MM/yyyy} " +
        //            $"has been cancelled successfully.",
        // });

        _dbContext.Notifications.Add(notification);

        await _dbContext.SaveChangesAsync();

        var result = new Response.CancelBookingResponse
        {
            Id = booking.Id,
            Status = booking.Status,
            CancelledAt = booking.CancelledAt ?? DateTime.UtcNow,
            Message = "Booking cancelled successfully",
        };

        return result;
    }
}