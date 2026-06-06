namespace Learnify.Core.Models;

public sealed class AiProviderException : InvalidOperationException
{
    public AiProviderException(
        string code,
        string provider,
        string message,
        string details,
        int statusCode,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Provider = provider;
        Details = details;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public string Provider { get; }
    public string Details { get; }
    public int StatusCode { get; }
}
