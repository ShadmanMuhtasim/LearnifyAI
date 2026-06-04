namespace Learnify.Application.Settings;

/// <summary>
/// Strongly-typed configuration class for AI provider settings.
/// Matches the "AiSettings" block in appsettings.json.
/// </summary>
public class AiSettings
{
    /// <summary>
    /// The active AI provider name (e.g., "Gemini", "OpenAI", "Ollama", "LocalOpenAI", "Claude").
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
    /// Local OpenAI-compatible provider settings for llama.cpp and similar servers.
    /// </summary>
    public LocalOpenAiSettings LocalOpenAI { get; set; } = new();

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
        /// The Gemini model name (e.g., "gemini-3.5-flash").
        /// </summary>
        public string Model { get; set; } = "gemini-3.5-flash";
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
        /// The base URL of the Ollama-compatible local server.
        /// </summary>
        public string BaseUrl { get; set; } = "http://127.0.0.1:8080";

        /// <summary>
        /// The Ollama model name (e.g., "llama3", "mistral", "phi3").
        /// </summary>
        public string Model { get; set; } = "llama3";
    }

    /// <summary>
    /// OpenAI-compatible local provider configuration.
    /// </summary>
    public class LocalOpenAiSettings
    {
        /// <summary>
        /// The base URL of the OpenAI-compatible local server.
        /// </summary>
        public string BaseUrl { get; set; } = "http://127.0.0.1:8080";

        /// <summary>
        /// The local model name exposed by the server.
        /// </summary>
        public string Model { get; set; } = "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf";

        /// <summary>
        /// Optional API key for local servers that require bearer auth.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
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
