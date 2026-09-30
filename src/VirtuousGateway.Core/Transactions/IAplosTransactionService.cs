namespace VirtuousGateway.Core.Transactions;

public interface IAplosTransactionService
{
    Task<string> CreateTransactionAsync(
        AplosTransactionRequest request,
        CancellationToken cancellationToken = default);
}