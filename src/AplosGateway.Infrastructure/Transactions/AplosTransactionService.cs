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
            throw new AplosPostNotDispatchedException(
                "Aplos transaction posting is disabled.");
        }

        string json;

        try
        {
            json =
                JsonSerializer.Serialize(request);
        }
        catch (Exception exception)
        {
            throw new AplosPostNotDispatchedException(
                "The Aplos transaction request could not be serialized and was not dispatched.",
                exception);
        }

        return await _aplosApiClient.PostAsync(
            "transactions",
            json,
            cancellationToken);
    }
}