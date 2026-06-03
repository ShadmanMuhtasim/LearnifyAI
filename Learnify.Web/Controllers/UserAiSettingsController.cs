using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Application.Settings;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/user/ai-settings")]
[Authorize]
public class UserAiSettingsController : ControllerBase
{
    private static readonly string[] AllowedProviders = ["gemini", "openai", "claude", "ollama"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly AiSettings _aiSettings;

    public UserAiSettingsController(
        IUnitOfWork unitOfWork,
        IOptions<AiSettings> aiSettings)
    {
        _unitOfWork = unitOfWork;
        _aiSettings = aiSettings.Value;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserAiSettingsResponseDTO>>> GetSettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _unitOfWork.UserAiSettings.GetByUserIdAsync(userId);

        return Ok(ApiResponse<UserAiSettingsResponseDTO>.Ok(
            BuildResponse(settings),
            "AI settings retrieved successfully."));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserAiSettingsResponseDTO>>> UpdateSettings(
        [FromBody] UpdateUserAiSettingsDTO request)
    {
        var provider = NormalizeProvider(request.ActiveProvider);
        if (!AllowedProviders.Contains(provider.ToLowerInvariant()))
        {
            return BadRequest(ApiResponse<UserAiSettingsResponseDTO>.BadRequest("Invalid provider name."));
        }

        var normalizedBaseUrl = "http://127.0.0.1:8080";
        if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) &&
            !TryNormalizeBaseUrl(request.OllamaBaseUrl, out normalizedBaseUrl, out var baseUrlError))
        {
            return BadRequest(ApiResponse<UserAiSettingsResponseDTO>.BadRequest(baseUrlError));
        }

        var userId = GetCurrentUserId();
        var settings = await _unitOfWork.UserAiSettings.GetByUserIdAsync(userId);
        var now = DateTime.UtcNow;
        var isNewSettings = false;

        if (settings is null)
        {
            isNewSettings = true;
            settings = new UserAiSettings
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = now
            };
            await _unitOfWork.UserAiSettings.AddAsync(settings);
        }

        settings.ActiveProvider = provider;
        settings.CustomModel = string.IsNullOrWhiteSpace(request.CustomModel)
            ? null
            : request.CustomModel.Trim();
        settings.OllamaBaseUrl = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? normalizedBaseUrl
            : null;

        if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            settings.ApiKey = null;
        }
        else if (request.ApiKey != null)
        {
            settings.ApiKey = string.IsNullOrWhiteSpace(request.ApiKey)
                ? null
                : request.ApiKey.Trim();
        }

        settings.UpdatedAt = now;

        if (!isNewSettings)
        {
            _unitOfWork.UserAiSettings.Update(settings);
        }

        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<UserAiSettingsResponseDTO>.Ok(
            BuildResponse(settings),
            "AI settings saved successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("User identity not found in token.");
    }

    private UserAiSettingsResponseDTO BuildResponse(UserAiSettings? settings)
    {
        if (settings == null)
        {
            return new UserAiSettingsResponseDTO
            {
                ActiveProvider = _aiSettings.ActiveProvider,
                Model = GetDefaultModel(_aiSettings.ActiveProvider),
                CustomModel = null,
                OllamaBaseUrl = _aiSettings.Ollama.BaseUrl,
                HasApiKey = HasDefaultApiKey(_aiSettings.ActiveProvider),
                IsDefault = true
            };
        }

        var provider = NormalizeProvider(settings.ActiveProvider);
        var model = string.IsNullOrWhiteSpace(settings.CustomModel)
            ? GetDefaultModel(provider)
            : settings.CustomModel!;

        return new UserAiSettingsResponseDTO
        {
            ActiveProvider = provider,
            Model = model,
            CustomModel = settings.CustomModel,
            OllamaBaseUrl = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
                ? settings.OllamaBaseUrl ?? _aiSettings.Ollama.BaseUrl
                : null,
            HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey) || HasDefaultApiKey(provider),
            IsDefault = false
        };
    }

    private string GetDefaultModel(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => _aiSettings.OpenAi.Model,
            "claude" => _aiSettings.Claude.Model,
            "ollama" => _aiSettings.Ollama.Model,
            _ => _aiSettings.Gemini.Model
        };

    private bool HasDefaultApiKey(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => !string.IsNullOrWhiteSpace(_aiSettings.OpenAi.ApiKey),
            "claude" => !string.IsNullOrWhiteSpace(_aiSettings.Claude.ApiKey),
            "gemini" => !string.IsNullOrWhiteSpace(_aiSettings.Gemini.ApiKey),
            _ => false
        };

    private static string NormalizeProvider(string provider)
    {
        if (provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            return "OpenAI";
        }

        if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("ollama / local llama", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("local llama", StringComparison.OrdinalIgnoreCase))
        {
            return "Ollama";
        }

        if (provider.Equals("claude", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        return "Gemini";
    }

    private static bool TryNormalizeBaseUrl(string? baseUrl, out string normalizedBaseUrl, out string error)
    {
        normalizedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://127.0.0.1:8080"
            : baseUrl.Trim().TrimEnd('/');
        error = "";

        if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "Ollama Base URL must be a full http:// or https:// URL.";
            return false;
        }

        return true;
    }
}
