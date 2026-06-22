namespace SWP391_AutoWashPro_BE.Service.Transaction;

public interface IService
{
    public Task<Response.GetTransactionResponse> GetTransactions(Request.GetTransactionsRequest request);
    public Task<Response.GetTransactionItems> GetTransactionById(Guid id);
}
