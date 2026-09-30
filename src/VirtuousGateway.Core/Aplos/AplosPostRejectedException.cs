using System.Net;

namespace VirtuousGateway.Core.Aplos;

public sealed class AplosPostRejectedException
    : Exception
{
    public AplosPostRejectedException(
        HttpStatusCode statusCode)
        : base(
            $"Aplos returned HTTP {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}