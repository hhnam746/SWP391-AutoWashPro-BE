using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;
using SWP391_AutoWashPro_BE.Service.Models;
using PersonalizedVoucherService = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;
using VoucherLifecycle = SWP391_AutoWashPro_BE.Service.Voucher.Lifecycle;

namespace SWP391_AutoWashPro_BE.Service.Booking;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Notification.IService _notificationService;
    private readonly PersonalizedVoucherService.IAudienceService _personalizedVoucherAudienceService;
    private readonly ILogger<Service> _logger;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContext,
        IServiceScopeFactory serviceScopeFactory,
        Notification.IService notificationService,
        PersonalizedVoucherService.IAudienceService personalizedVoucherAudienceService,
        ILogger<Service> logger)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _serviceScopeFactory = serviceScopeFactory;
        _notificationService = notificationService;
        _personalizedVoucherAudienceService = personalizedVoucherAudienceService;
        _logger = logger;
    }


    private static readonly TimeSpan DefaultUtcOffset = TimeSpan.FromHours(7);
    private const string RedeemPointValueConfigKey = "RedeemPointValue";
    private const string BookingProximityWarningConfigKey = "BookingProximityWarningMinutes";
    private const string BookingProximityWarningCode = "BOOKING_TIME_TOO_CLOSE";
    private int WorkingStartHour = 0;
    private int WorkingEndHour = 0;
    private int SlotDurationMinutes = 0;
    private int SlotBreakMinutes = 0;

    public async Task<Response.GetBookingSlotsResponse> GetBookingSlots(Guid branchId, DateOnly date)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new InvalidOperationException("User not found");

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
            throw new InvalidOperationException("WorkingStartHour config not found");
        }

        if (!int.TryParse(workingStartHourConfig.ConfigValue, out var workingStartHour))
        {
            throw new InvalidOperationException("Invalid WorkingStartHour config value");
        }

        WorkingStartHour = workingStartHour;

        var workingEndHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingEndHour");

        if (workingEndHourConfig == null)
        {
            throw new InvalidOperationException("WorkingEndHour config not found");
        }

        if (!int.TryParse(workingEndHourConfig.ConfigValue, out var workingEndHour))
        {
            throw new InvalidOperationException("Invalid WorkingEndHour config value");
        }

        WorkingEndHour = workingEndHour;

        var slotDurationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "SlotDurationMinutes")
                                 ?? throw new InvalidOperationException("SlotDurationMinutes config not found");

        if (!int.TryParse(slotDurationConfig.ConfigValue, out SlotDurationMinutes))
        {
            throw new InvalidOperationException("Invalid SlotDurationMinutes config value");
        }

        var slotBreakConfig = await _dbContext.SystemConfigs
                                  .FirstOrDefaultAsync(x => x.ConfigKey == "SlotBreakMinutes")
                              ?? throw new InvalidOperationException("SlotBreakMinutes config not found");

        if (!int.TryParse(slotBreakConfig.ConfigValue, out SlotBreakMinutes))
        {
            throw new InvalidOperationException("Invalid SlotBreakMinutes config value");
        }

        var slots = new List<Response.SlotStatus>();

        var currentTime = new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            WorkingStartHour,
            0,
            0,
            DefaultUtcOffset);

        var endWorkTime = new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            WorkingEndHour,
            0,
            0,
            DefaultUtcOffset);

        while (currentTime.AddMinutes(SlotDurationMinutes) <= endWorkTime)
        {
            var slotStartTime = currentTime;
            var slotEndTime = currentTime.AddMinutes(SlotDurationMinutes);

            var bookedSlot = bookings.FirstOrDefault(x =>
                x.StartTime.UtcDateTime == slotStartTime.UtcDateTime &&
                x.EndTime.UtcDateTime == slotEndTime.UtcDateTime);

            slots.Add(new Response.SlotStatus
            {
                StartTime = slotStartTime,
                EndTime = slotEndTime,
                Status = bookedSlot != null ? bookedSlot.Status : BookingStatus.Available
            });

            currentTime = slotEndTime.AddMinutes(SlotBreakMinutes);
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
            throw new InvalidOperationException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");

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
            throw new InvalidOperationException("WorkingStartHour config not found");
        }

        if (!int.TryParse(workingStartHourConfig.ConfigValue, out var workingStartHour))
        {
            throw new InvalidOperationException("Invalid WorkingStartHour config value");
        }

        WorkingStartHour = workingStartHour;

        var workingEndHourConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "WorkingEndHour");

        if (workingEndHourConfig == null)
        {
            throw new InvalidOperationException("WorkingEndHour config not found");
        }

        if (!int.TryParse(workingEndHourConfig.ConfigValue, out var workingEndHour))
        {
            throw new InvalidOperationException("Invalid WorkingEndHour config value");
        }

        WorkingEndHour = workingEndHour;

        var slotDurationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "SlotDurationMinutes")
                                 ?? throw new InvalidOperationException("SlotDurationMinutes config not found");

        if (!int.TryParse(slotDurationConfig.ConfigValue, out SlotDurationMinutes))
        {
            throw new InvalidOperationException("Invalid SlotDurationMinutes config value");
        }

        var slotBreakConfig = await _dbContext.SystemConfigs
                                  .FirstOrDefaultAsync(x => x.ConfigKey == "SlotBreakMinutes")
                              ?? throw new InvalidOperationException("SlotBreakMinutes config not found");

        if (!int.TryParse(slotBreakConfig.ConfigValue, out SlotBreakMinutes))
        {
            throw new InvalidOperationException("Invalid SlotBreakMinutes config value");
        }

        /////////////// Base Price Config /////////////
        var basePriceConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "BasePrice");

        if (basePriceConfig == null)
        {
            throw new InvalidOperationException("BasePrice config not found");
        }

        if (!decimal.TryParse(basePriceConfig.ConfigValue, out var basePrice))
        {
            throw new InvalidOperationException("Invalid BasePrice config value");
        }

        ///////////////////////////////// Sedan /////////////////////////////////////////////// 
        var SedanBaseConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "SedanBasePrice");


        if (SedanBaseConfig == null)
        {
            throw new InvalidOperationException("SedanBasePrice config not found");
        }

        if (!decimal.TryParse(SedanBaseConfig.ConfigValue, out var SedanBasePrice))
        {
            throw new InvalidOperationException("Invalid SedanBasePrice config value");
        }


        ///////////////////////////////// SUV ///////////////////////////////////////////////
        var SuvBaseConfig = await _dbContext.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == "SuvBasePrice");

        if (SuvBaseConfig == null)
        {
            throw new InvalidOperationException("SuvBasePrice config not found");
        }

        if (!decimal.TryParse(SuvBaseConfig.ConfigValue, out var SuvBasePrice))
        {
            throw new InvalidOperationException("Invalid SuvBasePrice config value");
        }

        var vehicle = await _dbContext.Vehicles
            .Include(x => x.VehicleType)
            .FirstOrDefaultAsync(x => x.Id == bookingRequest.VehicleId);
        if (vehicle == null)
        {
            throw new InvalidOperationException("Vehicle not found");
        }

        if (vehicle.VehicleType.TypeName == VehicleTypes.Sedan)
        {
            basePrice += SedanBasePrice;
        }
        else if (vehicle.VehicleType.TypeName == VehicleTypes.SUV)
        {
            basePrice += SuvBasePrice;
        }

        var bookingLocalStartTime = bookingRequest.StartTime.ToOffset(DefaultUtcOffset);

        if (bookingLocalStartTime <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Cannot book past time");
        }

        var bookingLocalDate = DateOnly.FromDateTime(bookingLocalStartTime.DateTime);
        if (bookingLocalDate != bookingRequest.BookingDate)
        {
            throw new InvalidOperationException("BookingDate must match StartTime date.");
        }

        var localStartTimeOnly = TimeOnly.FromDateTime(bookingLocalStartTime.DateTime);
        var workingStart = new TimeOnly(WorkingStartHour, 0);
        var workingEnd = new TimeOnly(WorkingEndHour, 0);

        if (localStartTimeOnly < workingStart || localStartTimeOnly >= workingEnd)
        {
            throw new InvalidOperationException($"StartTime must be within working hours ({workingStart}-{workingEnd}).");
        }

        if (bookingLocalStartTime.Second != 0 || bookingLocalStartTime.Millisecond != 0)
        {
            throw new InvalidOperationException("StartTime must be aligned to exact minute boundaries.");
        }

        var currentTime = new DateTimeOffset(
            bookingRequest.BookingDate.Year,
            bookingRequest.BookingDate.Month,
            bookingRequest.BookingDate.Day,
            WorkingStartHour,
            0,
            0,
            DefaultUtcOffset);

        var endWorkTime = new DateTimeOffset(
            bookingRequest.BookingDate.Year,
            bookingRequest.BookingDate.Month,
            bookingRequest.BookingDate.Day,
            WorkingEndHour,
            0,
            0,
            DefaultUtcOffset);

        DateTimeOffset? validSlotStart = null;
        DateTimeOffset? validSlotEnd = null;

        while (currentTime.AddMinutes(SlotDurationMinutes) <= endWorkTime)
        {
            var slotStartTime = currentTime;
            var slotEndTime = currentTime.AddMinutes(SlotDurationMinutes);

            if (slotStartTime.UtcDateTime == bookingLocalStartTime.UtcDateTime)
            {
                validSlotStart = slotStartTime;
                validSlotEnd = slotEndTime;
                break;
            }

            currentTime = slotEndTime.AddMinutes(SlotBreakMinutes);
        }

        if (!validSlotStart.HasValue || !validSlotEnd.HasValue)
        {
            throw new InvalidOperationException("StartTime must match a configured booking slot.");
        }

        var utcStartTime = bookingLocalStartTime.ToUniversalTime();
        var utcEndTime = validSlotEnd.Value.ToUniversalTime();

        var isBooked = await _dbContext.Bookings.AnyAsync(x =>
            x.BranchId == bookingRequest.BranchId && x.BookingDate == bookingRequest.BookingDate
                                                  && x.StartTime == utcStartTime
                                                  && x.Status != BookingStatus.Cancelled);
        if (isBooked)
        {
            throw new InvalidOperationException("Slot already booked");
        }

        var proximityWarningConfig = await _dbContext.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ConfigKey == BookingProximityWarningConfigKey)
            ?? throw new InvalidOperationException($"{BookingProximityWarningConfigKey} config not found");

        if (!int.TryParse(proximityWarningConfig.ConfigValue, out var proximityWarningMinutes) ||
            proximityWarningMinutes < 0)
        {
            throw new InvalidOperationException($"Invalid {BookingProximityWarningConfigKey} config value");
        }

        var activeBookingStatuses = new[]
        {
            BookingStatus.Pending,
            BookingStatus.Confirmed,
            BookingStatus.CheckIn,
            BookingStatus.InProgress
        };
        var warningWindowStart = utcStartTime.AddMinutes(-proximityWarningMinutes);
        var warningWindowEnd = utcEndTime.AddMinutes(proximityWarningMinutes);

        var existingScheduleConflicts = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerProfile.Id &&
                activeBookingStatuses.Contains(x.Status) &&
                x.EndTime >= warningWindowStart &&
                x.StartTime <= warningWindowEnd)
            .OrderBy(x => x.StartTime)
            .Select(x => new
            {
                BookingId = x.Id,
                x.BranchId,
                BranchName = x.Branch.Name,
                x.StartTime,
                x.EndTime
            })
            .ToListAsync();

        var scheduleConflicts = existingScheduleConflicts
            .Select(x =>
            {
                var gap = x.EndTime <= utcStartTime
                    ? utcStartTime - x.EndTime
                    : utcEndTime <= x.StartTime
                        ? x.StartTime - utcEndTime
                        : TimeSpan.Zero;

                return new Response.ScheduleConflict
                {
                    BookingId = x.BookingId,
                    BranchId = x.BranchId,
                    BranchName = x.BranchName,
                    StartTime = x.StartTime.ToOffset(DefaultUtcOffset),
                    EndTime = x.EndTime.ToOffset(DefaultUtcOffset),
                    IsSameBranch = x.BranchId == bookingRequest.BranchId,
                    GapMinutes = Math.Max(0, (int)Math.Floor(gap.TotalMinutes))
                };
            })
            .ToList();

        var acknowledgedConflictIds =
            bookingRequest.AcknowledgedScheduleConflictIds?.ToHashSet() ?? [];
        var hasUnacknowledgedConflict = scheduleConflicts
            .Any(x => !acknowledgedConflictIds.Contains(x.BookingId));

        if (hasUnacknowledgedConflict)
        {
            _logger.LogWarning(
                "Booking time proximity warning detected. CustomerId={CustomerId}, BranchId={BranchId}, StartTime={StartTime}, ConflictIds={ConflictIds}, WarningCode={WarningCode}.",
                customerProfile.Id,
                bookingRequest.BranchId,
                utcStartTime,
                scheduleConflicts.Select(x => x.BookingId).ToArray(),
                BookingProximityWarningCode);

            throw new BookingScheduleWarningException(new Response.ScheduleWarning
            {
                Code = BookingProximityWarningCode,
                Severity = "warning",
                ThresholdMinutes = proximityWarningMinutes,
                Conflicts = scheduleConflicts
            });
        }

        if (scheduleConflicts.Count > 0)
        {
            _logger.LogWarning(
                "Booking time proximity warning acknowledged. CustomerId={CustomerId}, BranchId={BranchId}, StartTime={StartTime}, ConflictIds={ConflictIds}, WarningCode={WarningCode}.",
                customerProfile.Id,
                bookingRequest.BranchId,
                utcStartTime,
                scheduleConflicts.Select(x => x.BookingId).ToArray(),
                BookingProximityWarningCode);
        }

        var canBooked = bookingLocalStartTime - DateTimeOffset.UtcNow;
        if ((int)canBooked.TotalDays > customerProfile.Tier.PriorityBookingDays)
        {
            throw new InvalidOperationException("Your rank is not enough for this booked");
        }

        //Discount Voucher
        var nowUtc = DateTimeOffset.UtcNow;
        var voucherDiscountAmount = (decimal)0;
        Repository.Entities.Voucher? voucher = null;

        if (bookingRequest.VoucherId.HasValue)
        {
            voucher = await _dbContext.Vouchers
                .AsNoTracking()
                .Include(x => x.PersonalizedVoucherIssuance)
                .FirstOrDefaultAsync(x => x.Id == bookingRequest.VoucherId.Value);

            if (voucher == null)
                throw new KeyNotFoundException("Voucher not found.");

            if (voucher.CustomerId != customerProfile.Id)
                throw new ForbiddenAccessException(
                    "Voucher does not belong to the current customer.");

            if (voucher.Status == VoucherStatus.Used || voucher.UsedAt.HasValue)
                throw new InvalidOperationException("Voucher already used.");

            if (voucher.Status == VoucherStatus.Reserved)
                throw new InvalidOperationException(
                    "Voucher is reserved by another booking.");

            if (voucher.Status == VoucherStatus.Expired ||
                voucher.ExpiresAt <= nowUtc)
                throw new InvalidOperationException("Voucher expired.");

            if (voucher.Status != VoucherStatus.Active)
                throw new InvalidOperationException("Voucher is not available.");

            if (voucher.DiscountValue <= 0)
                throw new InvalidOperationException(
                    "Voucher has no discount value.");

            if (voucher.PersonalizedVoucherIssuance != null &&
                PersonalizedVoucherService.PersonalizationPolicy.IsAcquisitionTrigger(
                    voucher.PersonalizedVoucherIssuance.TriggerType) &&
                await _dbContext.Bookings.AnyAsync(x => x.CustomerId == customerProfile.Id))
            {
                throw new InvalidOperationException(
                    "Welcome and no-first-booking vouchers can only be used for the customer's first booking.");
            }

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

        var eligiblePromotions = _dbContext.Promotions
            .Where(x =>
                x.IsActive &&
                !x.IsDeleted &&
                x.StartDate <= nowUtc &&
                x.EndDate > nowUtc);

        // Global Promotions
        var globalPromotions = await eligiblePromotions
            .Where(x => x.IsGlobal == true && x.IsDeleted == false)
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
        var tierPromotion = await eligiblePromotions
            .FirstOrDefaultAsync(x =>
                x.PromotionTiers.Any(promotionTier =>
                    promotionTier.TierId == customerProfile.TierId &&
                    !promotionTier.IsDeleted &&
                    !promotionTier.Tier.IsDeleted));

        if (tierPromotion != null)
        {
            var promotion = tierPromotion;

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

        // Redeem points use point units for storage and VND units for discount math.
        decimal redeemDiscountAmount = 0;
        int redeemPointsUsed = 0;

        if (bookingRequest.redemPoint == true)
        {
            var redeemPointValueConfig = await _dbContext.SystemConfigs
                                             .FirstOrDefaultAsync(x =>
                                                 x.ConfigKey == RedeemPointValueConfigKey)
                                        ?? throw new InvalidOperationException(
                                             $"{RedeemPointValueConfigKey} config not found");

            if (!int.TryParse(redeemPointValueConfig.ConfigValue, out var redeemPointValue) ||
                redeemPointValue <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid {RedeemPointValueConfigKey} config value");
            }

            var remainToDiscount = Math.Max(basePrice - discountAmount, 0);
            var maxRedeemablePoints = (int)Math.Floor(remainToDiscount / redeemPointValue);

            if (maxRedeemablePoints > 0)
            {
                redeemPointsUsed = Math.Min(customerProfile.TotalPoints, maxRedeemablePoints);
                redeemDiscountAmount = redeemPointsUsed * redeemPointValue;
            }
        }

        discountAmount += redeemDiscountAmount;

        if (customerProfile.TotalWashes > 0 && customerProfile.TotalWashes % 5 == 0)
        {
            discountAmount = basePrice;
        }

        //Wallet
        if (discountAmount > basePrice)
        {
            discountAmount = basePrice;
        }

        var finalPrice = basePrice - discountAmount;
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not exists");
        }

        var paymentDepositeConfig = await _dbContext.SystemConfigs
                                        .FirstOrDefaultAsync(x => x.ConfigKey == "PaymentDeposite")
                                    ?? throw new InvalidOperationException("PaymentDeposite config not found");

        if (!decimal.TryParse(paymentDepositeConfig.ConfigValue, out var paymentDeposite))
        {
            throw new InvalidOperationException("Invalid PaymentDeposite config value");
        }

        if (wallet.Balance - finalPrice * (paymentDeposite / 100) < 0)
        {
            throw new InvalidOperationException("Not enough balance");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        if (bookingRequest.VoucherId.HasValue)
        {
            var reservedCount = await VoucherLifecycle.ReserveAsync(
                _dbContext,
                bookingRequest.VoucherId.Value,
                customerProfile.Id,
                nowUtc);
            if (reservedCount != 1)
            {
                await VoucherLifecycle.ThrowReservationFailureAsync(
                    _dbContext,
                    bookingRequest.VoucherId.Value,
                    customerProfile.Id,
                    nowUtc);
            }
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
            RedemAmount = redeemPointsUsed,
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
                $"Your booking at {Branch.Name} has been confirmed for {bookingLocalStartTime:HH:mm dd/MM/yyyy}.",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Notifications.Add(notification);
        try
        {
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Slot already booked.");
        }

        //ProcessBookingReminder
        
        // _ = Task.Run(async () =>
        // {
        //     try
        //     {
        //         var reminderTime = utcStartTime.AddDays(-1);
        //
        //         var delayTime = reminderTime - DateTimeOffset.UtcNow;
        //
        //         if (delayTime > TimeSpan.Zero)
        //         {
        //             await Task.Delay(delayTime);
        //
        //             using var scope = _serviceScopeFactory.CreateScope();
        //
        //             var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //
        //             var reminderBooking = await dbContext.Bookings
        //                 .FirstOrDefaultAsync(x => x.Id == newBooking.Id);
        //
        //             if (reminderBooking != null &&
        //                 reminderBooking.Status == BookingStatus.Confirmed)
        //             {
        //                 var branch = await dbContext.Branches
        //                     .FirstOrDefaultAsync(x => x.Id == reminderBooking.BranchId);
        //
        //                 var customer = await dbContext.CustomerProfiles
        //                     .FirstOrDefaultAsync(x => x.Id == reminderBooking.CustomerId);
        //
        //                 if (customer != null)
        //                 {
        //                     var reminderNotification = new Repository.Entities.Notification()
        //                     {
        //                         Id = Guid.NewGuid(),
        //                         UserId = customer.UserId,
        //                         Type = NotificationType.BookingReminder,
        //                         Title = "Booking Reminder",
        //                         Content =
        //                             $"Reminder: Your booking at {branch?.Name ?? "our branch"} starts at {reminderBooking.StartTime.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}.",
        //                         IsRead = false,
        //                         CreatedAt = DateTimeOffset.UtcNow,
        //                     };
        //
        //                     dbContext.Notifications.Add(reminderNotification);
        //                     // await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
        //                     // {
        //                     //     UserId = userIdGuid,
        //                     //     Type = NotificationType.BookingCreated,
        //                     //     Data = $"Booking Success! Your booking at {branch?.Name ?? "our branch"} starts at {reminderBooking.StartTime.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}.",
        //                     // });
        //                     await dbContext.SaveChangesAsync();
        //                 }
        //             }
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine("======= Reminder Error " + e.Message + " ==========");
        //     }
        // });
        
        
        //ProcessAutoCancelJob
        
        //// Auto-cancel được xử lý tập trung bởi Quartz job ProcessBookingJob.
        
        
        // ================================= Auto Cancel Cronjob =================================
        // vì Task.Run(): bug when deploy render with TASK
        //using Background job to change Task solution
        //Task với Cronb job có cơ chế hoạt khác nhau
        //+ Lifecycle
        //Task: Dies instantly if your application crashes or restarts.
        //Cronb Job: Persists independently; will attempt to fire even if your main app is down.
        //+ Main Use Case: Tự tìm hiểu
        //+ Granularity: Tự tìm hiểu
        // ================================= End Auto Cancel Cronjob =================================
        //  _ = Task.Run(async () =>
        // {
        //     try
        //     {
        //         var cancelTimeConfig = await _dbContext.SystemConfigs
        //                                    .FirstOrDefaultAsync(x => x.ConfigKey == "CancelTimeMinutes")
        //                                ?? throw new Exception("CancelTimeMinutes config not found");

        //         if (!int.TryParse(cancelTimeConfig.ConfigValue, out var cancelTimeMinutes) ||
        //             cancelTimeMinutes < 0)
        //         {
        //             throw new Exception("Invalid CancelTimeMinutes config value");
        //         }

        //         var autoCancelTime = utcStartTime.AddMinutes(cancelTimeMinutes);
        //         var delayTime = autoCancelTime - DateTimeOffset.UtcNow;
        
        //         if (delayTime > TimeSpan.Zero)
        //         {
        //             await Task.Delay(delayTime);
        //         }
        
        //         using var scope = _serviceScopeFactory.CreateScope();
        //         var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        //         var booking = await dbContext.Bookings
        //             .FirstOrDefaultAsync(x => x.Id == newId);
        
        //         // Chỉ cancel nếu vẫn còn Confirmed (chưa CheckIn, chưa Cancel,...)
        //         if (booking != null && booking.Status == BookingStatus.Confirmed)
        //         {
        //             booking.Status = BookingStatus.Cancelled;
        //             // Gửi notification cho customer
        //             var customer = await dbContext.CustomerProfiles
        //                 .FirstOrDefaultAsync(x => x.Id == booking.CustomerId);
        
        //             if (customer != null)
        //             {
        //                 var branch = await dbContext.Branches
        //                     .FirstOrDefaultAsync(x => x.Id == booking.BranchId);
        
        //                 var cancelNotification = new Repository.Entities.Notification()
        //                 {
        //                     Id = Guid.NewGuid(),
        //                     UserId = customer.UserId,
        //                     Type = NotificationType.BookingCancelled,
        //                     Title = "Booking Auto-Cancelled",
        //                     Content = $"Your booking at {branch?.Name ?? "our branch"} on " +
        //                               $"{booking.StartTime.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy} " +
        //                               $"has been automatically cancelled due to no check-in.",
        //                     IsRead = false,
        //                     CreatedAt = DateTimeOffset.UtcNow,
        //                 };
        //                 await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
        //                 {
        //                     UserId = userIdGuid,
        //                     Type = NotificationType.BookingCancelled,
        //                     Data = $"Your booking at {branch?.Name ?? "our branch"} " +
        //                            $"for {booking.StartTime:HH:mm dd/MM/yyyy} " +
        //                            $"has been cancelled due to over check-in time.",
        //                 });
        
        //                 dbContext.Notifications.Add(cancelNotification);
        //             }
        
        //             await dbContext.SaveChangesAsync();
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine("======= Auto Cancel Error " + e.Message + " ==========");
        //     }
        // });
        
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
            StartTime = newBooking.StartTime.ToOffset(DefaultUtcOffset),
            EndTime = newBooking.EndTime.ToOffset(DefaultUtcOffset),
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
            throw new InvalidOperationException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");

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
        
        // query = query.OrderByDescending(x => x.UpdatedAt);

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
        var bookingItems = await bookingDetail.ToListAsync();
        foreach (var booking in bookingItems)
        {
            booking.StartTime = booking.StartTime.ToOffset(DefaultUtcOffset);
            booking.EndTime = booking.EndTime.ToOffset(DefaultUtcOffset);
        }

        var result = new Response.GetBookingsResponse
        {
            Data = bookingItems,
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

    public async Task<Response.ChatbotBookingSearchResponse> SearchMyBookingsForChatbot(
        string normalizedMessage,
        DateOnly? bookingDate,
        string? licensePlate,
        BookingStatus? status,
        bool hasBranchHint,
        bool hasLicensePlateHint,
        bool hasStatusHint,
        int limit = 5)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var customerProfile = await _dbContext.CustomerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        if (customerProfile == null)
        {
            throw new InvalidOperationException("Customer profile not found");
        }

        var requestedFilters = hasBranchHint || hasLicensePlateHint || hasStatusHint || bookingDate.HasValue;
        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.CustomerId == customerProfile.Id)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.BookingDate,
                x.StartTime,
                x.EndTime,
                BranchId = x.BranchId,
                BranchName = x.Branch.Name,
                BranchAddress = x.Branch.Address,
                LicensePlate = x.Vehicle.LicensePlate,
                x.FinalPrice
            })
            .ToListAsync();

        string? matchedBranchName = null;
        Guid? matchedBranchId = null;
        if (hasBranchHint)
        {
            var matchedBranch = bookings
                .Select(x => new
                {
                    x.BranchId,
                    x.BranchName,
                    NormalizedBranchName = NormalizeSearchText(x.BranchName)
                })
                .DistinctBy(x => x.BranchId)
                .Where(x => normalizedMessage.Contains(x.NormalizedBranchName, StringComparison.Ordinal))
                .OrderByDescending(x => x.NormalizedBranchName.Length)
                .FirstOrDefault();

            matchedBranchName = matchedBranch?.BranchName;
            matchedBranchId = matchedBranch?.BranchId;
        }

        var matchedLicensePlate = hasLicensePlateHint ? NormalizePlate(licensePlate) : null;
        var matchedStatus = hasStatusHint ? status : null;
        var matchedBookingDate = bookingDate;
        var hasResolvedFilters = matchedBranchId.HasValue ||
                                 matchedBookingDate.HasValue ||
                                 !string.IsNullOrWhiteSpace(matchedLicensePlate) ||
                                 matchedStatus.HasValue;

        if (requestedFilters && !hasResolvedFilters)
        {
            return new Response.ChatbotBookingSearchResponse
            {
                MatchedBranch = null,
                MatchedLicensePlate = null,
                MatchedStatus = null,
                HasRequestedFilters = true,
                HasResolvedFilters = false,
                Message = "Mình chưa xác định được rõ chi nhánh, biển số hoặc trạng thái booking từ câu hỏi của bạn.",
                TotalMatched = 0,
                Items = []
            };
        }

        var filteredBookings = bookings.AsEnumerable();
        if (matchedBranchId.HasValue)
        {
            filteredBookings = filteredBookings.Where(x => x.BranchId == matchedBranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(matchedLicensePlate))
        {
            filteredBookings = filteredBookings.Where(x => PlateMatches(x.LicensePlate, matchedLicensePlate));
        }

        if (matchedBookingDate.HasValue)
        {
            filteredBookings = filteredBookings.Where(x => x.BookingDate == matchedBookingDate.Value);
        }

        if (matchedStatus.HasValue)
        {
            filteredBookings = filteredBookings.Where(x => x.Status == matchedStatus.Value);
        }

        var orderedBookings = filteredBookings
            .OrderByDescending(x => x.StartTime)
            .ToList();

        var bookingItems = orderedBookings
            .Take(limit)
            .Select(x => new Response.ChatbotBookingItem
            {
                Id = x.Id,
                Status = x.Status,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime.ToOffset(DefaultUtcOffset),
                EndTime = x.EndTime.ToOffset(DefaultUtcOffset),
                BranchName = x.BranchName,
                BranchAddress = x.BranchAddress,
                LicensePlate = x.LicensePlate,
                FinalPrice = x.FinalPrice
            })
            .ToList();

        return new Response.ChatbotBookingSearchResponse
        {
            MatchedBranch = matchedBranchName,
            MatchedLicensePlate = matchedLicensePlate,
            MatchedStatus = matchedStatus,
            HasRequestedFilters = requestedFilters,
            HasResolvedFilters = hasResolvedFilters,
            Message = requestedFilters && orderedBookings.Count == 0
                ? "Bạn chưa có booking nào khớp với bộ lọc đang hỏi."
                : null,
            TotalMatched = orderedBookings.Count,
            Items = bookingItems
        };
    }

    public async Task<Response.GetBookingDetailResponse> GetBookingById(Guid bookingId)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new InvalidOperationException("User not found");
        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");
        var query = await _dbContext.Bookings
            .Include(x => x.Branch)
            .Include(x => x.Vehicle)
            .Include(x => x.Voucher)
            .FirstOrDefaultAsync(x =>
                x.CustomerId == customerProfile.Id &&
                x.Id == bookingId);
        if (query == null)
        {
            throw new InvalidOperationException("Booking not found");
        }

        var Voucher = query.Voucher == null
            ? null
            : new Response.VoucherInfo()
            {
                Id = query.VoucherId,
                Code = query.Voucher.Code,
                DiscountAmount = query.Voucher.DiscountValue
            };
        var result = new Response.GetBookingDetailResponse
        {
            Id = query.Id,
            Status = query.Status,
            BookingDate = query.BookingDate,
            StartTime = query.StartTime.ToOffset(DefaultUtcOffset),
            EndTime = query.EndTime.ToOffset(DefaultUtcOffset),
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
            DiscountAmount = query.DiscountAmount,
            FinalPrice = query.FinalPrice,
        };
        return result;
    }

    private static bool PlateMatches(string sourcePlate, string filterPlate)
    {
        var normalizedSource = NormalizePlate(sourcePlate);
        if (string.IsNullOrWhiteSpace(normalizedSource) || string.IsNullOrWhiteSpace(filterPlate))
        {
            return false;
        }

        return normalizedSource.Contains(filterPlate, StringComparison.OrdinalIgnoreCase) ||
               filterPlate.Contains(normalizedSource, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePlate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                'đ' => 'd',
                _ => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? character : ' '
            });
        }

        return string.Join(" ", builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public async Task<Response.CheckInBookingResponse> CheckInBooking(
        Guid Id,
        CancellationToken cancellationToken = default)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new InvalidOperationException("User not found");

        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new InvalidOperationException("Customer profile not found");

        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(x =>
                x.Id == Id &&
                x.CustomerId == customerProfile.Id &&
                x.Status == BookingStatus.Confirmed);

        if (booking == null)
        {
            throw new InvalidOperationException(
                "Booking not found, does not belong to the current customer, or is no longer confirmed.");
        }

        return await ProcessCheckInBooking(
            booking,
            customerProfile,
            userIdGuid,
            cancellationToken);
    }

    public async Task<Response.CheckInBookingResponse> CheckInBookingByAdmin(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bookings can be checked in.");
        }

        var customerProfile = await _dbContext.CustomerProfiles
            .Include(x => x.Tier)
            .FirstOrDefaultAsync(x => x.Id == booking.CustomerId);

        if (customerProfile == null)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        return await ProcessCheckInBooking(
            booking,
            customerProfile,
            customerProfile.UserId,
            cancellationToken);
    }

    private async Task<Response.CheckInBookingResponse> ProcessCheckInBooking(
        Repository.Entities.Booking booking,
        Repository.Entities.CustomerProfile customerProfile,
        Guid customerUserId,
        CancellationToken cancellationToken)
    {
        var cancelTimeConfig = await _dbContext.SystemConfigs
                                   .FirstOrDefaultAsync(x => x.ConfigKey == "CancelTimeMinutes")
                               ?? throw new InvalidOperationException("CancelTimeMinutes config not found");

        if (!int.TryParse(cancelTimeConfig.ConfigValue, out var cancelTimeMinutes) ||
            cancelTimeMinutes < 0)
        {
            throw new InvalidOperationException("Invalid CancelTimeMinutes config value");
        }
        var now = DateTimeOffset.UtcNow;
        var latestCheckInTime = booking.StartTime.AddMinutes(cancelTimeMinutes);

        if (now < booking.StartTime)
        {
            throw new InvalidOperationException("Check-in is not available before the booking start time.");
        }

        if (now > latestCheckInTime)
        {
            throw new InvalidOperationException("Check-in time has expired.");
        }
        var msg = "";
        Guid? upgradedTierId = null;
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);
        var voucher = booking.VoucherId.HasValue
            ? await _dbContext.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == booking.VoucherId.Value,
                    cancellationToken)
            : null;
        if (booking.VoucherId.HasValue && voucher == null)
        {
            throw new InvalidOperationException("Booking voucher not found.");
        }

        if (voucher != null &&
            (voucher.Status != VoucherStatus.Reserved || voucher.UsedAt.HasValue))
        {
            throw new InvalidOperationException(
                "Booking voucher is no longer reserved.");
        }

        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }

        var paymentDepositeConfig = await _dbContext.SystemConfigs
                                        .FirstOrDefaultAsync(x => x.ConfigKey == "PaymentDeposite")
                                    ?? throw new InvalidOperationException("PaymentDeposite config not found");

        if (!decimal.TryParse(paymentDepositeConfig.ConfigValue, out var paymentDeposite))
        {
            throw new InvalidOperationException("Invalid PaymentDeposite config value");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (wallet.Balance - (booking.FinalPrice - (booking.FinalPrice * (paymentDeposite / 100))) < 0)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = now;
            booking.UpdatedAt = now;
            if (booking.VoucherId.HasValue)
            {
                await VoucherLifecycle.ReleaseReservedAsync(
                    _dbContext,
                    booking.VoucherId.Value,
                    booking.Id,
                    now,
                    cancellationToken);
            }
            msg = "Check-in Failed";
        }
        else
        {
            if (booking.VoucherId.HasValue)
            {
                var usedCount = await VoucherLifecycle.MarkReservedAsUsedAsync(
                    _dbContext,
                    booking.VoucherId.Value,
                    booking.Id,
                    now,
                    cancellationToken);
                if (usedCount != 1)
                {
                    throw new InvalidOperationException(
                        "Booking voucher is no longer reserved.");
                }
            }

            var remainingAmount = booking.FinalPrice - (booking.FinalPrice * (paymentDeposite / 100));
            wallet.Balance -= remainingAmount;
            booking.Status = BookingStatus.InProgress;
            booking.UpdatedAt = now;
            msg = "Check-in successful";

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
                                   ?? throw new InvalidOperationException("BonusPoint config not found");

            if (!int.TryParse(bonusPointConfig.ConfigValue, out var bonusPoint))
            {
                throw new InvalidOperationException("Invalid BonusPoint config value");
            }

            customerProfile.TotalWashes += 1;
            customerProfile.TotalPoints += bonusPoint;
            var earnPointTransaction = new Repository.Entities.PointTransaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerProfile.Id,
                Customer = customerProfile,
                BookingId = booking.Id,
                Booking = booking,
                Points = bonusPoint,
                TransactionType = PointTransactionType.Earn,
                Description = $"Earned {bonusPoint} points from booking.",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.PointTransactions.Add(earnPointTransaction);
            var currentTier = _dbContext.Tiers.FirstOrDefault(x => x.Level == customerProfile.Tier.Level);
            var nextTier = _dbContext.Tiers.FirstOrDefault(x => x.Level == customerProfile.Tier.Level + 1);

            if (nextTier != null &&
                customerProfile.TotalWashes >= nextTier.RequiredWashes)
            {
                customerProfile.TierId = nextTier.Id;
                customerProfile.Tier = nextTier;
                upgradedTierId = nextTier.Id;
                var notification = new Repository.Entities.Notification()
                {
                    Id = Guid.NewGuid(),
                    UserId = customerUserId,
                    Type = NotificationType.TierUpgraded,
                    Title = "Tier Upgraded",
                    Content =
                        $"Congratulations! Your tier has been upgraded from {currentTier.Name} to {nextTier.Name}.",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                // await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest
                // {
                //     UserId = customerUserId,
                //     Type = NotificationType.TierUpgraded,
                //     Data = $"Congratulations! Your tier has been upgraded from {currentTier.Name} to {nextTier.Name}."
                // });
                _dbContext.Notifications.Add(notification);
            }

            if ((booking.RedemAmount ?? 0) > 0)
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
                    Description = $"Redeemed {booking.RedemAmount} points for booking discount.",
                    CreatedAt = DateTime.UtcNow,
                };
                _dbContext.PointTransactions.Add(pointTransaction);
            }
            
            //Thêm background job => ProcessBookingAutoComplete 
            //Completed khi mà time(NOW) > Endtime
            
            //
            // _ = Task.Run(async () =>
            // {
            //     try
            //     {
            //         await Task.Delay(TimeSpan.FromMinutes(SlotDurationMinutes));
            //         using var scope = _serviceScopeFactory.CreateScope();
            //         var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //         var delayedBooking = await dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == booking.Id);
            //         if (delayedBooking != null && delayedBooking.Status == BookingStatus.InProgress)
            //         {
            //             delayedBooking.Status = BookingStatus.Completed;
            //             delayedBooking.CompletedAt = DateTime.UtcNow;
            //             var branch = dbContext.Branches.FirstOrDefault(x => x.Id == delayedBooking.BranchId);
            //             var customer =
            //                 dbContext.CustomerProfiles.FirstOrDefault(x => x.Id == delayedBooking.CustomerId);
            //             if (customer != null)
            //             {
            //                 var notification = new Repository.Entities.Notification()
            //                 {
            //                     Id = Guid.NewGuid(),
            //                     UserId = customer.UserId,
            //                     Type = NotificationType.BookingCompleted,
            //                     Title = "Booking Completed",
            //                     Content =
            //                         $"Your booking at {branch?.Name ?? "our branch"} has been completed successfully. Thank you for using our service.",
            //                     IsRead = false,
            //                     CreatedAt = DateTimeOffset.UtcNow,
            //                 };
            //                 dbContext.Notifications.Add(notification);
            //             }
            //
            //             await dbContext.SaveChangesAsync();
            //         }
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine("=======Error Occure " + e.Message + " ==========");
            //     }
            // });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (upgradedTierId.HasValue)
        {
            try
            {
                await _personalizedVoucherAudienceService.ProcessTierUpgradeAsync(
                    customerProfile.Id,
                    upgradedTierId.Value,
                    booking.Id,
                    cancellationToken);
            }
            catch
            {
                _logger.LogWarning(
                    "Tier upgrade personalization failed after check-in commit. CustomerId={CustomerId}, TriggerType={TriggerType}, CycleKey={CycleKey}, BookingId={BookingId}, ErrorCode={ErrorCode}.",
                    customerProfile.Id,
                    PersonalizedVoucherTriggerType.TierUpgrade,
                    PersonalizedVoucherService.PersonalizationPolicy.CreateTierUpgradeCycleKey(
                        upgradedTierId.Value),
                    booking.Id,
                    "TIER_UPGRADE_PERSONALIZATION_FAILED");
            }
        }

        var result = new Response.CheckInBookingResponse
        {
            Id = booking.Id,
            Status = booking.Status,
            CheckedInAt = booking.StartTime.ToOffset(DefaultUtcOffset),
            EstimatedCompletedAt = booking.EndTime.ToOffset(DefaultUtcOffset),
            Message = msg
        };
        return result;
    }

    public async Task<Response.CancelBookingResponse> CancelBooking(
        Guid Id,
        Request.CancelBookingRequest bookingRequest)
    {
        if (bookingRequest == null)
        {
            throw new ArgumentException("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(bookingRequest.Reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var customerProfile = await _dbContext.CustomerProfiles
                                  .FirstOrDefaultAsync(x => x.UserId == userIdGuid)
                              ?? throw new InvalidOperationException("Customer profile not found");

        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(x =>
                x.Id == Id &&
                x.CustomerId == customerProfile.Id);
        if (booking == null)
        {
            throw new InvalidOperationException("Booking not found");
        }
        

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Booking has already been cancelled");
        }

        if (booking.Status == BookingStatus.InProgress ||
            booking.Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException("This booking cannot be cancelled");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bookings can be cancelled");
        }

        var cancellationConfig = await _dbContext.SystemConfigs
                                     .FirstOrDefaultAsync(x => x.ConfigKey == "CancellationDeadlineHours")
                                 ?? throw new InvalidOperationException("CancellationDeadlineHours config not found");

        if (!int.TryParse(
                cancellationConfig.ConfigValue,
                out var cancellationDeadlineHours) ||
            cancellationDeadlineHours < 0)
        {
            throw new InvalidOperationException("Invalid CancellationDeadlineHours config value");
        }

        var now = DateTimeOffset.UtcNow;
        var cancellationDeadline =
            booking.StartTime.AddHours(-cancellationDeadlineHours);

        if (now >= cancellationDeadline)
        {
            throw new InvalidOperationException(
                $"Booking must be cancelled at least {cancellationDeadlineHours} hours before the start time.");
        }
        var totalPaidAmount = await RefundWorkflow.GetTotalPaidAmountAsync(_dbContext, booking.Id);
        var refundDecision = RefundWorkflow.CalculateCustomerCancellationRefund(
            now,
            booking.StartTime,
            cancellationDeadlineHours,
            totalPaidAmount);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.UpdatedAt = now;
        if (booking.VoucherId.HasValue)
        {
            await VoucherLifecycle.ReleaseReservedAsync(
                _dbContext,
                booking.VoucherId.Value,
                booking.Id,
                now);
        }

        Guid? refundTransactionId = null;
        if (refundDecision.RefundApplied)
        {
            var wallet = await _dbContext.Wallets
                .FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id);

            if (wallet == null)
            {
                throw new InvalidOperationException("Wallet not found");
            }

            var walletBalanceBefore = wallet.Balance;
            var walletBalanceAfter = walletBalanceBefore + refundDecision.RefundAmount;

            wallet.Balance = walletBalanceAfter;
            wallet.UpdatedAt = now;

            var refundTransaction = RefundWorkflow.CreateRefundTransaction(
                booking,
                customerProfile.Id,
                refundDecision.RefundAmount,
                refundDecision.ReasonCode,
                $"Refund for booking cancellation: {bookingRequest.Reason.Trim()}",
                walletBalanceBefore,
                walletBalanceAfter,
                now);

            refundTransactionId = refundTransaction.Id;
            _dbContext.Transactions.Add(refundTransaction);
        }

        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == booking.BranchId);

        var refundMessage = refundDecision.RefundApplied
            ? $" Refund amount: {refundDecision.RefundAmount:N0} VND has been returned to your wallet."
            : " No refund was applied because the cancellation deadline has passed.";

        var notification = new Repository.Entities.Notification()
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            Type = NotificationType.BookingCancelled,
            Title = "Booking Cancelled",
            Content =
                $"Your booking at {branch?.Name ?? "our branch"} " +
                $"for {booking.StartTime.ToOffset(DefaultUtcOffset):HH:mm dd/MM/yyyy} " +
                $"has been cancelled successfully.{refundMessage}",
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
        await transaction.CommitAsync();

        var result = new Response.CancelBookingResponse
        {
            Id = booking.Id,
            Status = booking.Status,
            CancelledAt = (booking.CancelledAt ?? DateTimeOffset.UtcNow).ToOffset(DefaultUtcOffset),
            RefundApplied = refundDecision.RefundApplied,
            RefundAmount = refundDecision.RefundAmount,
            RefundTransactionId = refundTransactionId,
            RefundReasonCode = refundDecision.ReasonCode,
            Message = refundDecision.RefundApplied
                ? "Booking cancelled successfully and refund applied"
                : "Booking cancelled successfully",
        };

        return result;
    }
}
