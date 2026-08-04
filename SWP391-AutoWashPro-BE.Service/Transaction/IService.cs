namespace SWP391_AutoWashPro_BE.Service.Transaction;

public interface IService
{
    public Task<Response.GetTransactionResponse> GetTransactions(Request.GetTransactionsRequest request);
    public Task<Response.GetTransactionItems> GetTransactionById(Guid id);
    public Task<Response.GetTransactionResponse> GetTransactionsV2(Request.GetTransactionsRequest request);
    public Task<Response.GetTransactionItems> GetTransactionByIdV2(Guid id);
}
