using Learnify.Core.Models;

namespace Learnify.Web.Controllers;

public sealed record AiErrorResponse(
    bool Success,
    string ErrorCode,
    string? Provider,
    string Message,
    string? Details)
{
    public static AiErrorResponse FromException(AiProviderException exception) =>
        new(false, exception.Code, exception.Provider, exception.Message, exception.SafeDetails);
}
