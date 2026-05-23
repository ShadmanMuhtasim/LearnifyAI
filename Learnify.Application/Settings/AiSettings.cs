namespace Learnify.Application.Settings;

/// <summary>
/// Strongly-typed configuration class for AI provider settings.
/// Matches the "AiSettings" block in appsettings.json.
/// </summary>
public class AiSettings
{
    /// <summary>
    /// The active AI provider name (e.g., "Gemini", "OpenAI", "Ollama", "Claude").
    /// </summary>
    public string ActiveProvider { get; set; } = "Gemini";

    /// <summary>
    /// Google Gemini provider settings.
    /// </summary>
    public GeminiSettings Gemini { get; set; } = new();

    /// <summary>
    /// OpenAI provider settings.
    /// </summary>
    public OpenAiSettings OpenAi { get; set; } = new();

    /// <summary>
    /// Ollama provider settings.
    /// </summary>
    public OllamaSettings Ollama { get; set; } = new();

    /// <summary>
    /// Anthropic Claude provider settings.
    /// </summary>
    public ClaudeSettings Claude { get; set; } = new();

    /// <summary>
    /// Google Gemini provider configuration.
    /// </summary>
    public class GeminiSettings
    {
        /// <summary>
        /// The API key for Google Gemini.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The Gemini model name (e.g., "gemini-1.5-flash").
        /// </summary>
        public string Model { get; set; } = "gemini-1.5-flash";
    }

    /// <summary>
    /// OpenAI provider configuration.
    /// </summary>
    public class OpenAiSettings
    {
        /// <summary>
        /// The API key for OpenAI.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The OpenAI model name (e.g., "gpt-4o-mini").
        /// </summary>
        public string Model { get; set; } = "gpt-4o-mini";
    }

    /// <summary>
    /// Ollama provider configuration.
    /// </summary>
    public class OllamaSettings
    {
        /// <summary>
        /// The base URL of the Ollama server (e.g., "http://localhost:11434").
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// The Ollama model name (e.g., "llama3", "mistral", "phi3").
        /// </summary>
        public string Model { get; set; } = "llama3";
    }

    /// <summary>
    /// Anthropic Claude provider configuration.
    /// </summary>
    public class ClaudeSettings
    {
        /// <summary>
        /// The API key for Anthropic Claude.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The Claude model name (e.g., "claude-sonnet-4-20250514").
        /// </summary>
        public string Model { get; set; } = "claude-sonnet-4-20250514";

        /// <summary>
        /// The Anthropic API version (e.g., "2023-06-01").
        /// </summary>
        public string ApiVersion { get; set; } = "2023-06-01";
    }
}