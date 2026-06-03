namespace Learnify.Application.DTOs;

public class UserAiSettingsResponseDTO
{
    public string ActiveProvider { get; set; } = "Gemini";
    public string Model { get; set; } = "gemini-3.5-flash";
    public string? CustomModel { get; set; }
    public string? OllamaBaseUrl { get; set; }
    public bool HasApiKey { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateUserAiSettingsDTO
{
    public string ActiveProvider { get; set; } = "Gemini";
    public string? ApiKey { get; set; }
    public string? CustomModel { get; set; }
    public string? OllamaBaseUrl { get; set; }
}
