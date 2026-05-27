namespace SWP391_AutoWashPro_BE.Service.Promotion;

public interface IService
{
    public Task<Base.Response.PageResult<Response.PromotionResponse>> GetPromotion(string? searchTerm, int pageSize,
        int pageIndex);
    
    public Task<string> CreatePromotion(Request.PromotionRequest request);
    public Task<string> UpdatePromotion(Guid id, Request.UpdatePromotionRequest request);
    public Task<string> UpdatePromotionStatus(
        Guid id,
        Request.UpdatePromotionStatusRequest request);

    public Task<string> DeletePromotion(Guid id);
}