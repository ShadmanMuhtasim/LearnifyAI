namespace Learnify.Core.Models;

public sealed class AiProviderException : Exception
{
    public AiProviderException(
        string code,
        string message,
        string provider,
        int statusCode,
        string? safeDetails = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Provider = provider;
        StatusCode = statusCode;
        SafeDetails = safeDetails;
    }

    public string Code { get; }
    public string Provider { get; }
    public int StatusCode { get; }
    public string? SafeDetails { get; }
}
