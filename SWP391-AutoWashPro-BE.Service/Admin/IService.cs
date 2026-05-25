namespace SWP391_AutoWashPro_BE.Service.Admin;

public interface IService
{
    public Task<List<Response.BranchResponse>> GetBranches(bool? isActive, string? keyword);
    public Task<string> CreateBranch(Request.CreateBranch request);
    public Task<string> UpdateBranch(Guid id, Request.UpdateBranch request);
    public Task<string> DeleteBranch(Guid id);
    public Task<Response.DashboardResponse> GetDashboard(Request.GetDashboardRequest request);
    public Task<string> UpdateUserVerificationStatus(Guid userId);
    public Task<Base.Response.PageResult<Response.AllProfileResponse>> GetAllUserProfile(
        string? searchTerm,
        int pageSize,
        int pageIndex);
    public Task<Base.Response.PageResult<Response.AllProfileResponse>> GetUsersNeedVerification(
        string? searchTerm,
        int pageSize,
        int pageIndex);
    public Task<Response.GetUserByIdResponse> GetUserById(Guid userId);
    public Task<Response.GetUserStatusResponse> GetUserStatusById(Guid userId);
    public Task<string> UpdateUserStatusById(Guid userId, Request.UpdateUserByStatusRequest request);

    public Task<List<Response.BookingResponse>> GetBookings(
        Request.GetBookingRequest request);
    public Task<Response.BookingSlotResponse> GetBookingSlots(Request.GetBookingSlotRequest request);
    public Task<Response.RevenueReportResponse> GetRevenueReport(Request.GetRevenueReportRequest request);
    public Task<Base.Response.PageResult<Response.BranchReportItemResponse>> GetBranchReport(Request.GetBranchReportRequest request);
    public Task<Response.LoyaltyReportResponse> GetLoyaltyReport(Request.GetLoyaltyReportRequest request);
    public Task<Response.CompleteBookingByAdminResponse> CompleteBookingByAdmin(
        Guid bookingId,
        Request.CompleteBookingByAdminRequest request);
    public Task<Response.CancelBookingByAdminResponse> CancelBookingByAdmin(
        Guid bookingId,
        Request.CancelBookingByAdminRequest request);
    
}
