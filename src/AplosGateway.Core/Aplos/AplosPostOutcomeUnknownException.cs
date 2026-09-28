namespace AplosGateway.Core.Aplos;

public sealed class AplosPostOutcomeUnknownException
    : Exception
{
    public AplosPostOutcomeUnknownException(
        string message,
        Exception? innerException = null)
        : base(
            message,
            innerException)
    {
    }
}