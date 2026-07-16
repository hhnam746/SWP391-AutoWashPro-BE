namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public interface IRuleService
{
    Task<Base.Response.PageResult<Response.RuleResponse>> GetRulesAsync(
        Request.GetRulesRequest request,
        CancellationToken cancellationToken = default);
    Task<Response.RuleResponse> CreateRuleAsync(
        Request.RuleRequest request,
        CancellationToken cancellationToken = default);
    Task<Response.RuleResponse> UpdateRuleAsync(
        Guid id,
        Request.RuleRequest request,
        CancellationToken cancellationToken = default);
    Task<Response.RuleResponse> UpdateRuleStatusAsync(
        Guid id,
        Request.UpdateRuleStatusRequest request,
        CancellationToken cancellationToken = default);
    Task<List<Response.ReportItem>> GetReportAsync(
        Request.ReportRequest request,
        CancellationToken cancellationToken = default);
}
