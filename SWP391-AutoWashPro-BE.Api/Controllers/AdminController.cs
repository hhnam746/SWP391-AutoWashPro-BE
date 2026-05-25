using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Admin;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = JwtExtensions.AdminPolicy)]
public class AdminController : ControllerBase
{
    private readonly IService _adminService;

    public AdminController(IService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] bool? isActive, [FromQuery] string? keyword)
    {
        var result = await _adminService.GetBranches(isActive, keyword);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get branches", HttpContext.TraceIdentifier));
    }
    
    //CRUD branch by Admin
    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch(Request.CreateBranch request)
    {
        var result = await _adminService.CreateBranch(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create branch successfully", HttpContext.TraceIdentifier));
    }
    
    [HttpPatch("branches/{id:guid}")]
    public async Task<IActionResult> UpdateBranch(Guid id, Request.UpdateBranch request)
    {
        var result = await _adminService.UpdateBranch(id, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update branch successfully", HttpContext.TraceIdentifier));
    }
    
    [HttpDelete("branches/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _adminService.DeleteBranch(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Delete branch successfully", HttpContext.TraceIdentifier));
    }
    
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Request.GetDashboardRequest request)
    {
        var result = await _adminService.GetDashboard(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get dashboard", HttpContext.TraceIdentifier));
    }

    [HttpGet("reports/revenue")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] Request.GetRevenueReportRequest request)
    {
        var result = await _adminService.GetRevenueReport(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get revenue report", HttpContext.TraceIdentifier));
    }

    [HttpGet("reports/branches")]
    public async Task<IActionResult> GetBranchReport([FromQuery] Request.GetBranchReportRequest request)
    {
        var result = await _adminService.GetBranchReport(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get branch report", HttpContext.TraceIdentifier));
    }

    [HttpGet("reports/loyalty")]
    public async Task<IActionResult> GetLoyaltyReport([FromQuery] Request.GetLoyaltyReportRequest request)
    {
        var result = await _adminService.GetLoyaltyReport(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get loyalty report", HttpContext.TraceIdentifier));
    }
    
    
    
    //Using admin to verify user
    [HttpPatch("users/{id:guid}/verify")]
    public async Task<IActionResult> UpdateUserVerificationStatus(Guid id)
    {
        var result = await _adminService.UpdateUserVerificationStatus(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Admin verification status updated", HttpContext.TraceIdentifier));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(string? searchTerm, int pageIndex = 1, int pageSize = 10)
    {
        var result = await _adminService.GetAllUserProfile(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get all users", HttpContext.TraceIdentifier));
    }

    [HttpGet("users/pending-verification")]
    public async Task<IActionResult> GetUsersNeedVerification(string? searchTerm, int pageIndex = 1, int pageSize = 10)
    {
        var result = await _adminService.GetUsersNeedVerification(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get users pending verification", HttpContext.TraceIdentifier));
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings([FromBody] Request.GetBookingRequest request)
    {
        var result = await _adminService.GetBookings(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get bookings", HttpContext.TraceIdentifier));
    }

    [HttpGet("booking-slots")]
    public async Task<IActionResult> GetBookingSlots([FromBody] Request.GetBookingSlotRequest request)
    {
        var result = await _adminService.GetBookingSlots(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get booking slots", HttpContext.TraceIdentifier));
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _adminService.GetUserById(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get user by id", HttpContext.TraceIdentifier));
    }

    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatusById(
        [FromRoute] Guid id,
        [FromBody] Request.UpdateUserByStatusRequest request)
    {
        var result = await _adminService.UpdateUserStatusById(id, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update user status", HttpContext.TraceIdentifier));
    }

    [HttpGet("users/{id:guid}/status")]
    public async Task<IActionResult> GetUserStatusById(Guid id)
    {
        var result = await _adminService.GetUserStatusById(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get user status", HttpContext.TraceIdentifier));
    }
}
