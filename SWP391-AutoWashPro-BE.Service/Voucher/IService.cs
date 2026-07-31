namespace SWP391_AutoWashPro_BE.Service.Voucher;

public interface IService
{
    public Task<Base.Response.PageResult<Response.VoucherResponse>> GetVoucher(Guid userId,int pageSize, int pageIndex);

    public Task<Base.Response.PageResult<Response.CustomerVoucherResponse>> GetMyVouchers(
        int pageSize,
        int pageIndex,
        CancellationToken cancellationToken = default);

    public Task<Base.Response.PageResult<Response.CustomerVoucherResponse>> GetAvailableVouchers(
        int pageSize,
        int pageIndex,
        CancellationToken cancellationToken = default);

    public Task<Response.ValidateVoucherResponse> ValidateVoucher(
        Guid userId,
        Request.ValidateVoucherRequest request);
}
