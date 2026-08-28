using System.Text.Json;
using AplosGateway.Core.Aplos;
using AplosGateway.Core.Configuration;
using AplosGateway.Core.Transactions;
using Microsoft.Extensions.Options;

namespace AplosGateway.Infrastructure.Transactions;

public sealed class AplosTransactionService
    : IAplosTransactionService
{
    private readonly IAplosApiClient _aplosApiClient;
    private readonly AplosOptions _options;

    public AplosTransactionService(
        IAplosApiClient aplosApiClient,
        IOptions<AplosOptions> options)
    {
        _aplosApiClient = aplosApiClient;
        _options = options.Value;
    }

    public async Task<string> CreateTransactionAsync(
        AplosTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.AllowTransactionPosting)
        {
            throw new InvalidOperationException(
                "Aplos transaction posting is disabled.");
        }

        var json =
            JsonSerializer.Serialize(request);

        return await _aplosApiClient.PostAsync(
            "transactions",
            json,
            cancellationToken);
    }
}