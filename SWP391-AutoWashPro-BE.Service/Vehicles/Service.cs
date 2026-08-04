using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.PostgresTypes;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Vehicles;

public class Service : IService
{

    private static readonly BookingStatus[] ActiveBookingStatuses =
    [
        BookingStatus.Pending,
        BookingStatus.Confirmed,
        BookingStatus.CheckIn,
        BookingStatus.InProgress
    ];

    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly MediaService.IService _mediaService;

    private async Task<Repository.Entities.CustomerProfile> GetRequiredCustomerProfileAsync()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

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

    private static bool IsDuplicateLicensePlateViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException
            && postgresException.SqlState == "23505"
            && string.Equals(postgresException.ConstraintName, "IX_vehicle_license_plate", StringComparison.Ordinal);
    }

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext, MediaService.IService mediaService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _mediaService = mediaService;
    }

    public async Task<Response.GetVehiclesResponse> GetVehicles(int page, int pageSize)
    {
        ValidatePagination(page, pageSize);

        var customerProfile = await GetRequiredCustomerProfileAsync();

        var query = _dbContext.Vehicles
            .Include(x => x.VehicleType)
            .Where(x => x.CustomerId == customerProfile.Id && x.IsActive == true);
        
        var selectedQuery = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.VehicleListItemResponse
            {
                Id = x.Id,
                LicensePlate = x.LicensePlate,
                Brand = x.Brand,
                Model = x.Model,
                Color = x.Color,
                IsActive = x.IsActive,
                HasActiveBooking = _dbContext.Bookings.Any(b =>
                    b.VehicleId == x.Id &&
                    ActiveBookingStatuses.Contains(b.Status)),
                VehicelType = x.VehicleType.TypeName,
                VehicleImages = _dbContext.VehicleImages.Where(i => i.VehicleId == x.Id).Select(mp => new Response.VehicleImageResponse
                {
                    Id = mp.Id,
                    ImageUrl = mp.ImageUrl,
                }).ToList(),
            });

        var totalCount = await query.CountAsync();
        var result = new Response.GetVehiclesResponse
        {
            Data = await selectedQuery.ToListAsync(),
            Pagination = new Response.PaginationResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            }
        };

        return result;
    }
    public async Task<Response.CreateVehicleResponse> CreateVehicle(Request.CreateVehicleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customerProfile = await GetRequiredCustomerProfileAsync();

        var normalizedLicensePlate = request.LicensePlate?.Trim();

        // Exception note: phát sinh khi request thiếu biển số xe hoặc chỉ chứa khoảng trắng.
        if (string.IsNullOrWhiteSpace(normalizedLicensePlate))
            throw new ArgumentException("License plate is required.", nameof(request.LicensePlate));

        // Exception note: phát sinh khi request không gửi ảnh verify hoặc số lượng ảnh vượt ngoài giới hạn 1-3.
        if (request.VehicleImages == null || request.VehicleImages.Count is < 1 or > 3)
            throw new ArgumentException("Vehicle images must contain from 1 to 3 files.", nameof(request.VehicleImages));

        if (await _dbContext.Vehicles.AnyAsync(x => x.CustomerId == customerProfile.Id && x.LicensePlate == normalizedLicensePlate && x.IsActive))
        {
            // Exception note: phát sinh khi khách hàng tạo trùng biển số xe đang còn hoạt động.
            throw new InvalidOperationException("License plate already exists.");
        }

        var vehicleType = await _dbContext.VehicleTypes
            .FirstOrDefaultAsync(x => x.TypeName == request.VehicleType);

        if (vehicleType is null)
        {
            throw new Exception("Vehicle type not found");
        }

        List<string> uploadedVehicleImageUrls;
        try
        {
            uploadedVehicleImageUrls = new List<string>();
            foreach (var vehicleImage in request.VehicleImages)
            {
                var uploadedVehicleImageUrl = await _mediaService.UploadImageAsync(vehicleImage);
                uploadedVehicleImageUrls.Add(uploadedVehicleImageUrl);
            }
        }
        catch (Exception ex)
        {
            // Exception note: phát sinh khi upload ảnh verify thất bại do input ảnh không hợp lệ hoặc lỗi media service.
            throw new InvalidOperationException("Vehicle image upload failed.", ex);
        }

        var newVehile = new Repository.Entities.Vehicle()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            LicensePlate = normalizedLicensePlate,
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim(),
            VehicleType = vehicleType,
            Color = request.Color?.Trim(),
            IsActive = true,
        };

        var vehicleImages = uploadedVehicleImageUrls
            .Select(imageUrl => new Repository.Entities.VehicleImage
            {
                Id = Guid.NewGuid(),
                VehicleId = newVehile.Id,
                ImageUrl = imageUrl,
                IsActive = true,
            })
            .ToList();

        try
        {
            _dbContext.Vehicles.Add(newVehile);
            _dbContext.VehicleImages.AddRange(vehicleImages);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateLicensePlateViolation(ex))
        {
            // Exception note: phát sinh khi DB từ chối lưu do biển số xe đã vi phạm unique constraint.
            throw new InvalidOperationException("License plate already exists.");
        }
        catch (DbUpdateException ex)
        {
            // Exception note: phát sinh khi lưu vehicle và vehicle images vi phạm ràng buộc DB khác hoặc lỗi cập nhật DB.
            throw new InvalidOperationException("Failed to save vehicle to the database.", ex);
        }

        var result = new Response.CreateVehicleResponse
        {
            Id = newVehile.Id,
            LicensePlate = newVehile.LicensePlate,
            Brand = newVehile.Brand,
            Model = newVehile.Model,
            Color = newVehile.Color,
            VehicleType = newVehile.VehicleType.TypeName,
            IsActive = newVehile.IsActive,
        };

        return result;
    }

    public async Task<Response.GetVehicleByIdResponse> GetVehicleById(Guid id)
    {
        var customerProfile = await GetRequiredCustomerProfileAsync();

        var query = await _dbContext.Vehicles
            .Include(x => x.VehicleType)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerProfile.Id && x.IsActive);
        if (query == null)
        {
            // Exception note: phát sinh khi vehicle không tồn tại, đã bị soft delete hoặc không thuộc về customer hiện tại.
            throw new KeyNotFoundException("Vehicle not found.");
        }
        var vehicelImagesQuery = _dbContext.VehicleImages.Where(x => x.VehicleId == id);
        var vehicelImages = vehicelImagesQuery.Select(x => new Response.VehicleImageResponse
        {
            Id = x.Id,
            ImageUrl = x.ImageUrl
        });
        var result = new Response.GetVehicleByIdResponse
        {
            Id = query.Id,
            LicensePlate = query.LicensePlate,
            Brand = query.Brand,
            Model = query.Model,
            Color = query.Color,
            IsActive = query.IsActive,
            VehicleType = query.VehicleType.TypeName,
            VehicleImages = vehicelImages.ToList() ,
        };

        return result;
    }

    public async Task<Response.UpdateVehicleResponse> UpdateVehicle(Guid id, Request.UpdateVehicleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customerProfile = await GetRequiredCustomerProfileAsync();

        var vehicle = await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerProfile.Id && x.IsActive);

        if (vehicle == null)
        {
            // Exception note: phát sinh khi vehicle không tồn tại, đã bị soft delete hoặc không thuộc về customer hiện tại.
            throw new KeyNotFoundException("Vehicle not found.");
        }

        vehicle.Brand = request.Brand?.Trim() ?? vehicle.Brand;
        vehicle.Model = request.Model?.Trim() ?? vehicle.Model;
        vehicle.Color = request.Color?.Trim() ?? vehicle.Color;
        var vehicleType = await _dbContext.VehicleTypes
            .FirstOrDefaultAsync(x => x.TypeName == request.VehicleType);

        if (vehicleType is null)
        {
            throw new Exception("Vehicle type not found");
        }
        vehicle.VehicleType = vehicleType;

        try
        {
            _dbContext.Vehicles.Update(vehicle);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Exception note: phát sinh khi cập nhật vehicle thất bại do lỗi ràng buộc hoặc lỗi ghi DB.
            throw new InvalidOperationException("Failed to update vehicle.", ex);
        }

        var result = new Response.UpdateVehicleResponse
        {
            Id = vehicle.Id,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Color = vehicle.Color,
            VehicleType = vehicle.VehicleType.TypeName,
        };

        return result;
    }

    public async Task<Response.DeleteVehicleResponse> DeleteVehicle(Guid id)
    {
        var customerProfile = await GetRequiredCustomerProfileAsync();

        var vehicle = await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerProfile.Id && x.IsActive);
        if (vehicle == null)
        {
            // Exception note: phát sinh khi vehicle không tồn tại, đã bị soft delete hoặc không thuộc về customer hiện tại.
            throw new KeyNotFoundException("Vehicle not found.");
        }

        var hasActiveBookings = await _dbContext.Bookings.AnyAsync(x =>
            x.VehicleId == vehicle.Id &&
            ActiveBookingStatuses.Contains(x.Status));

        // Exception note: phát sinh khi vehicle đang còn booking active nên không được phép soft delete.
        if (hasActiveBookings)
            throw new InvalidOperationException("Cannot delete vehicle with active bookings.");

        vehicle.IsActive = false;
        vehicle.DeletedAt = DateTimeOffset.UtcNow;

        try
        {
            _dbContext.Vehicles.Update(vehicle);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Exception note: phát sinh khi soft delete vehicle thất bại do lỗi ràng buộc hoặc lỗi ghi DB.
            throw new InvalidOperationException("Failed to delete vehicle.", ex);
        }

        var result = new Response.DeleteVehicleResponse
        {
            Message = "Successfully deleted vehicle",
        };

        return result;

    }
}