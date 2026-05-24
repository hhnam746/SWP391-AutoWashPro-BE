using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Booking;

public class Service : IService
{
    
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    
    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }
    
    

    public async Task<Response.GetBookingSlotsResponse> GetBookingSlots(Guid BranchId, DateOnly Date)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        var books = _dbContext.Bookings.Where(x => x.BranchId == BranchId);
        var booksDate = await books.Where(x => x.BookingDate == Date).ToListAsync();
        
        var slotInfor = booksDate.Select(x => new Response.SlotStatus
        {
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            Status = x.Status
        });

        var result = new Response.GetBookingSlotsResponse
        {
            BranchId = BranchId,
            Date = Date,
            SlotDurationMinutes = 15,
            Data = slotInfor.ToList()
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
        var discountAmount = _dbContext.Vouchers.FirstOrDefault(x => x.Id == bookingRequest.VoucherId).DiscountValue;
        var basePrice = 80000;
        // Thieu point redem point
        var newId = Guid.NewGuid();
        var newBooking = new Repository.Entities.Booking()
        {
            Id = newId,
            BranchId = bookingRequest.BranchId,
            VehicleId = bookingRequest.VehicleId,
            BookingDate = bookingRequest.BookingDate,
            StartTime = bookingRequest.StartTime,
            BasePrice = basePrice,
            DiscountAmount = discountAmount,
            FinalPrice = basePrice - discountAmount,
            CreatedAt =  DateTime.Now
        };
        var status = "";
        var wallet = await  _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == user.Id);
        if (wallet == null)
        {
            throw new Exception("Wallet not exists");
        }

        if (wallet.Balance - basePrice >= 0)
        {
            throw new Exception("Not enough balance");
        }

        wallet.Balance -= basePrice - (basePrice * (30 / 100));
        _dbContext.Bookings.Add(newBooking);
        await _dbContext.SaveChangesAsync();
        var result = new Response.CreateBookingResponse
        {
            Id = newId,
            Status = "Confirmed",
            Branch = new Response.BookingBranch
            {
                Id = bookingRequest.BranchId,
                Name = _dbContext.Branches.FirstOrDefault(x => x.Id == bookingRequest.BranchId).Name,
            },
            Vehicle = new Response.BookingVehicle
            {
                Id = bookingRequest.VehicleId,
                LicensePlate = _dbContext.Vehicles.FirstOrDefault(x => x.Id == bookingRequest.VehicleId).LicensePlate,
            },
            BookingDate = bookingRequest.BookingDate,
            StartTime = bookingRequest.StartTime,
            EndTime = bookingRequest.StartTime.AddMinutes(15),
            BasePrice = basePrice,
            DiscountAmount = discountAmount,
            FinalPrice = basePrice - discountAmount,
        };
        return result;
    }
    
    public async Task<Response.GetBookingsResponse> GetBookings(BookingStatus? status, DateOnly fromDate, DateOnly toDate, int page, int pageSize)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        
        var query = _dbContext.Bookings.Where(x => x.CustomerId == user.Id);
        if (!status.HasValue)
        {
            query = query.Where(x => x.Status ==  status);
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
        var result = new Response.GetBookingsResponse
        {
            Data = bookingDetail.ToList(),
            Pagination = new Response.Pagination
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalCount / pageSize,
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
        var query = await _dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
        var result = new Response.GetBookingDetailResponse
        {
            Id = query.Id,
            Status = query.Status,
            BookingDate = query.BookingDate,
            StartTime = query.StartTime,
            EndTime = query.EndTime,
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
            Voucher = new Response.VoucherInfo
            {
                Id = query.VoucherId,
                Code = query.Voucher.Code,
                DiscountAmount = query.Voucher.DiscountValue
            },
            BasePrice = query.BasePrice,
            DiscountAmount = query.Voucher.DiscountValue,
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
        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == Id);
        if (booking == null)
        {
            throw new Exception("Booking not found");
        }

        var checkInAt = DateTime.Now;
        var msg = "";
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.CustomerId == user.Id);
        if (wallet == null)
        {
            throw new Exception("Wallet not found");
        }
        
        wallet.Balance -= booking.FinalPrice;
        if (wallet.Balance - booking.FinalPrice < 0)
        {
            booking.Status = BookingStatus.Cancelled;
            msg = "Check-in Failed";
        }
        else
        {
            booking.Status = BookingStatus.InProgress;
            msg = "Check-in In Success";
        }

        await _dbContext.SaveChangesAsync();
        var result = new Response.CheckInBookingResponse
        {
            Id = booking.Id,
            Status = booking.Status,
            CheckedInAt = checkInAt,
            EstimatedCompletedAt = checkInAt.AddMinutes(15),
            Message = msg
        };
        return result;
    }

    public async Task<Response.CancelBookingResponse> CancelBooking(Guid Id, Request.CancelBookingRequest bookingRequest)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user == null)
            throw new Exception("User not found");
        
        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == Id);
        if (booking == null)
        {
            throw new Exception("Booking not found");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new Exception("Booking has been cancelled before");
        }

        if ((booking.BookingDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber) < 1)
        {
            throw new Exception("Too Close to be Cancelled");
        }
        booking.Status = BookingStatus.Cancelled;
        await _dbContext.SaveChangesAsync();
        var result = new Response.CancelBookingResponse
        {
            Id = Id,
            Status = booking.Status,
            CancelledAt = DateTime.Now,
            Message = "Cancelled successfully",
        };
        return result;
    }
}