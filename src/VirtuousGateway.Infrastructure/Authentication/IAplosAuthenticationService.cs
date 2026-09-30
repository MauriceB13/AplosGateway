namespace VirtuousGateway.Core.Authentication;

public interface IAplosAuthenticationService
{
    Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);
}