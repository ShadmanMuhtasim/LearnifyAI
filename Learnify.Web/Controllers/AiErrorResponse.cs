using Learnify.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Learnify.Web.Controllers;

internal static class AiErrorResponse
{
    private const string GeminiRateLimitMessage =
        "Gemini quota or rate limit was reached. Try again later, switch provider, or use a local/free mode if available.";

    public static ObjectResult FromException(
        ControllerBase controller,
        Exception exception,
        ILogger logger,
        string action,
        string provider = "AI provider")
    {
        var classified = Classify(exception, provider);
        logger.LogWarning(
            exception,
            "AI provider failure. Provider={Provider}, Action={Action}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, Details={Details}",
            classified.Provider,
            action,
            classified.StatusCode,
            classified.Code,
            classified.Details);

        return controller.StatusCode(
            classified.StatusCode,
            new
            {
                success = false,
                code = classified.Code,
                provider = classified.Provider,
                message = classified.Message,
                details = classified.Details
            });
    }

    private static ClassifiedAiError Classify(Exception exception, string provider)
    {
        if (exception is AiProviderException providerException)
        {
            return new ClassifiedAiError(
                providerException.StatusCode,
                providerException.Code,
                providerException.Provider,
                providerException.Code == "AI_RATE_LIMIT" ? GeminiRateLimitMessage : providerException.Message,
                SafePreview(providerException.Details));
        }

        var message = exception.Message;
        var resolvedProvider = message.Contains("Gemini", StringComparison.OrdinalIgnoreCase)
            ? "Gemini"
            : provider;

        if (message.Contains("API key is not configured", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassifiedAiError(
                StatusCodes.Status400BadRequest,
                "AI_CONFIG_MISSING",
                resolvedProvider,
                message,
                "");
        }

        if (IsRateLimitMessage(message))
        {
            return new ClassifiedAiError(
                StatusCodes.Status429TooManyRequests,
                "AI_RATE_LIMIT",
                resolvedProvider,
                resolvedProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase)
                    ? GeminiRateLimitMessage
                    : message,
                SafePreview(message));
        }

        return new ClassifiedAiError(
            StatusCodes.Status502BadGateway,
            "AI_PROVIDER_UNAVAILABLE",
            resolvedProvider,
            message,
            SafePreview(message));
    }

    private static bool IsRateLimitMessage(string message)
        => message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("exceeded your current quota", StringComparison.OrdinalIgnoreCase);

    private static string SafePreview(string value)
    {
        var compact = string.Join(" ", value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 240 ? compact : compact[..240];
    }

    private sealed record ClassifiedAiError(
        int StatusCode,
        string Code,
        string Provider,
        string Message,
        string Details);
}
