using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Service.Base;

namespace SWP391_AutoWashPro_BE.Service.Vehicles;

public class Service : IService
{

    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly MediaService.IService _mediaService;
    
    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext, MediaService.IService mediaService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _mediaService = mediaService;
    }

    public async Task<Response.GetVehiclesResponse> GetVehicles(int page, int pageSize)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");
        
        var query = _dbContext.Vehicles.Where(x => x.CustomerId == customerProfile.Id && x.IsActive == true);
        var selectedQuery = query.Select(x => new Response.VehicleListItemResponse
        {
            Id = x.Id,
            LicensePlate = x.LicensePlate,
            Brand = x.Brand,
            Model = x.Model,
            Color = x.Color,
            IsActive = x.IsActive,
            HasActiveBooking = _dbContext.Bookings.Any(b => b.VehicleId == x.Id)
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
                TotalPages = totalCount / pageSize,
            }
        };
        return result;
    }

    public async Task<Response.CreateVehicleResponse> CreateVehicle(Request.CreateVehicleRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        var newVehile = new Repository.Entities.Vehicle()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerProfile.Id,
            LicensePlate = request.LicensePlate,
            Brand = request.Brand,
            Model = request.Model,
            Color = request.Color,
            IsActive = true,
        };
        _dbContext.Vehicles.Add(newVehile);
        await _dbContext.SaveChangesAsync();
        var result = new Response.CreateVehicleResponse
        {
            Id = newVehile.Id,
            LicensePlate = await _mediaService.UploadImageAsync(request.LicensePlateImageUrl),
            Brand = newVehile.Brand,
            Model = newVehile.Model,
            Color = newVehile.Color,
            IsActive = newVehile.IsActive,
        };
        return result;
    }

    public async Task<Response.GetVehicleByIdResponse> GetVehicleById(Guid id)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        var query = await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.CustomerId == customerProfile.Id && x.IsActive == true);
        if (query == null)
        {
            throw new Exception("Vehicle not found");
        }

        var result = new Response.GetVehicleByIdResponse
        {
            Id = query.Id,
            LicensePlate = query.LicensePlate,
            Brand = query.Brand,
            Model = query.Model,
            Color = query.Color,
            IsActive = query.IsActive,
        };
        return result;
    }

    public async Task<Response.UpdateVehicleResponse> UpdateVehicle(Guid id, Request.UpdateVehicleRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");

        var Vehicle = await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (Vehicle == null)
        {
            throw new Exception("Vehicle not found");
        }
        Vehicle.Brand = request.Brand ?? Vehicle.Brand;
        Vehicle.Model = request.Model ?? Vehicle.Model;
        Vehicle.Color = request.Color ?? Vehicle.Color;
        _dbContext.Vehicles.Update(Vehicle);
        await _dbContext.SaveChangesAsync();

        var result = new Response.UpdateVehicleResponse
        {
            Id = Vehicle.Id,
            Brand = Vehicle.Brand,
            Model = Vehicle.Model,
            Color = Vehicle.Color,
        };
        return result;
    }

    public async Task<Response.DeleteVehicleResponse> DeleteVehicle(Guid id)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
           .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
            throw new Exception("User not found");
        var customerProfile = await _dbContext.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
        if (customerProfile == null)
            throw new Exception("Customer profile not found");
        var Vehicle = await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (Vehicle == null)
        {
            throw new Exception("Vehicle not found");
        }
        Vehicle.IsActive = false;
        _dbContext.Vehicles.Update(Vehicle);
        await _dbContext.SaveChangesAsync();
        var result = new Response.DeleteVehicleResponse
        {
            Message = "Successfully deleted vehicle",
        };
        return result;

    }
}