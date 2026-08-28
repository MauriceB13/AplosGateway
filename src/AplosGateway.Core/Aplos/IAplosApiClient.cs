namespace AplosGateway.Core.Aplos;

public interface IAplosApiClient
{
    Task<string> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task<string> PostAsync(
        string relativePath,
        string jsonContent,
        CancellationToken cancellationToken = default);
}