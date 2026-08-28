using System.Text.Json;
using AplosGateway.Core.Aplos;
using AplosGateway.Core.Transactions;

namespace AplosGateway.Infrastructure.Transactions;

public sealed class AplosTransactionService
    : IAplosTransactionService
{
    private readonly IAplosApiClient _aplosApiClient;

    public AplosTransactionService(
        IAplosApiClient aplosApiClient)
    {
        _aplosApiClient = aplosApiClient;
    }

    public async Task<string> CreateTransactionAsync(
        AplosTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var json =
            JsonSerializer.Serialize(request);

        return await _aplosApiClient.PostAsync(
            "transactions",
            json,
            cancellationToken);
    }
}