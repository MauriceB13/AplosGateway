namespace AplosGateway.Core.Aplos;

public sealed class AplosPostNotDispatchedException
    : Exception
{
    public AplosPostNotDispatchedException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}