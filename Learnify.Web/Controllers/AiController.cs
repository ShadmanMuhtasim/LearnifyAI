using Learnify.Application.DTOs.AI;
using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Learnify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private static readonly string[] AllowedProviders = ["gemini", "openai", "claude", "ollama", "mock"];

    private readonly IAiService _aiService;
    private readonly UserAiSettingsStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AiSettings _aiSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiService aiService,
        UserAiSettingsStore store,
        IUnitOfWork unitOfWork,
        IOptions<AiSettings> aiSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _store = store;
        _unitOfWork = unitOfWork;
        _aiSettings = aiSettings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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

    [HttpGet("provider")]
    public async Task<IActionResult> GetProviderStatus()
    {
        var userId = GetCurrentUserId();
        var settings = await _unitOfWork.UserAiSettings.GetByUserIdAsync(userId);

        if (settings == null)
        {
            var defaultStatus = _store.GetUserStatus(userId);
            return Ok(defaultStatus);
        }

        var provider = NormalizeProviderName(settings.ActiveProvider);
        var model = string.IsNullOrWhiteSpace(settings.CustomModel)
            ? GetDefaultModel(provider)
            : settings.CustomModel;
        var baseUrl = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? settings.OllamaBaseUrl ?? _aiSettings.Ollama.BaseUrl
            : "";

        return Ok(new
        {
            mode = "own",
            provider,
            activeProvider = provider,
            model,
            baseUrl,
            remainingDefaultRequests = _store.GetRemainingRequests(userId)
        });
    }

    [HttpPut("provider/user")]
    public IActionResult SetUserProvider([FromBody] UserProviderRequest request)
    {
        var provider = NormalizeProviderName(request.Provider);
        if (!AllowedProviders.Contains(provider.ToLowerInvariant()))
        {
            return BadRequest(new { message = "Invalid provider name." });
        }

        if (!TryNormalizeProviderSettings(provider, request.Model, request.BaseUrl, out var model, out var baseUrl, out var error))
        {
            return BadRequest(new { message = error });
        }

        var userId = GetCurrentUserId();
        _store.SetUserConfig(
            userId,
            provider,
            provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ? "" : request.ApiKey,
            model,
            baseUrl);

        return Ok(new
        {
            success = true,
            provider,
            model,
            baseUrl,
            message = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
                ? "Switched to Ollama / Local LLaMA."
                : $"Switched to {provider} with your own credentials."
        });
    }

    [HttpDelete("provider/user")]
    public IActionResult RevertToDefault()
    {
        var userId = GetCurrentUserId();
        _store.ClearUserConfig(userId);
        return Ok(new { success = true, message = "Reverted to default provider." });
    }

    [HttpPut("provider/admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult SetAdminProvider([FromBody] AdminProviderRequest request)
    {
        var provider = NormalizeProviderName(request.Provider);
        if (!AllowedProviders.Contains(provider.ToLowerInvariant()))
        {
            return BadRequest(new { message = "Invalid provider name." });
        }

        if (!TryNormalizeProviderSettings(provider, request.Model, request.BaseUrl, out var model, out var baseUrl, out var error))
        {
            return BadRequest(new { message = error });
        }

        _store.SetAdminDefault(
            provider,
            provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ? "" : request.ApiKey,
            model,
            baseUrl);

        return Ok(new
        {
            success = true,
            provider,
            model,
            baseUrl,
            message = "Default provider updated for all users."
        });
    }

    [HttpPost("provider/test")]
    public async Task<IActionResult> TestProvider(
        [FromBody] ProviderTestRequest request,
        CancellationToken cancellationToken)
    {
        var provider = NormalizeProviderName(request.Provider);
        if (!provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Only Ollama / Local LLaMA connection tests are supported." });
        }

        if (!TryNormalizeBaseUrl(request.BaseUrl, out var baseUrl, out var error))
        {
            return BadRequest(new { success = false, message = error });
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(baseUrl);

        if (await CanGetAsync(httpClient, "/api/tags", timeoutCts.Token))
        {
            return Ok(new
            {
                success = true,
                provider = "Ollama",
                baseUrl,
                compatibleApi = "ollama",
                message = $"Connected to an Ollama-compatible local server at {baseUrl}."
            });
        }

        if (await CanGetAsync(httpClient, "/v1/models", timeoutCts.Token))
        {
            return Ok(new
            {
                success = false,
                provider = "Ollama",
                baseUrl,
                compatibleApi = "openai",
                message = $"OpenAI-compatible local server detected at {baseUrl}, but the current local provider uses Ollama-compatible endpoints."
            });
        }

        return Ok(new
        {
            success = false,
            provider = "Ollama",
            baseUrl,
            compatibleApi = "unknown",
            message = $"Local LLaMA server not reachable at {baseUrl}."
        });
    }

    [HttpPost("summarize")]
    public async Task<IActionResult> Summarize(
        [FromBody] SummarizeNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { success = false, message = "Content cannot be empty." });
        }

        try
        {
            var summary = await _aiService.SummarizeNoteAsync(request.Content, cancellationToken);
            return Ok(new SummarizeNoteResponse(request.NoteId, summary, DateTime.UtcNow));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing note '{NoteId}'", request.NoteId);
            return StatusCode(500, new { success = false, message = "Failed to summarize note." });
        }
    }

    [HttpPost("flashcards")]
    public async Task<IActionResult> GenerateFlashcards(
        [FromBody] FlashcardRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { success = false, message = "Content cannot be empty." });
        }

        try
        {
            var flashcards = await _aiService.GenerateFlashcardsAsync(
                request.Content,
                request.Count,
                cancellationToken);

            var response = new FlashcardResponse(
                request.NoteId,
                flashcards.Select(f => new FlashcardItem(f.Question, f.Answer)).ToList(),
                DateTime.UtcNow);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flashcards for note '{NoteId}'", request.NoteId);
            return StatusCode(500, new { success = false, message = "Failed to generate flashcards." });
        }
    }

    [HttpPost("study-tips")]
    public async Task<IActionResult> GetStudyTips(
        [FromBody] StudyTipsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new { success = false, message = "Topic cannot be empty." });
        }

        try
        {
            var tips = await _aiService.GetStudyTipsAsync(request.Topic, cancellationToken);
            return Ok(new StudyTipsResponse(tips, DateTime.UtcNow));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating study tips for topic '{Topic}'", request.Topic);
            return StatusCode(500, new { success = false, message = "Failed to generate study tips." });
        }
    }

    private static string NormalizeProviderName(string provider)
    {
        if (provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            return "OpenAI";
        }

        if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            return "Ollama";
        }

        if (provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return "Gemini";
        }

        if (provider.Equals("claude", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        if (provider.Equals("mock", StringComparison.OrdinalIgnoreCase))
        {
            return "Mock";
        }

        return provider.Trim();
    }

    private string GetDefaultModel(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "openai" => _aiSettings.OpenAi.Model,
            "claude" => _aiSettings.Claude.Model,
            "ollama" => _aiSettings.Ollama.Model,
            _ => _aiSettings.Gemini.Model
        };

    private static bool TryNormalizeProviderSettings(
        string provider,
        string model,
        string baseUrl,
        out string normalizedModel,
        out string normalizedBaseUrl,
        out string error)
    {
        normalizedModel = string.IsNullOrWhiteSpace(model) && provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? "llama3"
            : model.Trim();
        normalizedBaseUrl = "";
        error = "";

        if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryNormalizeBaseUrl(baseUrl, out normalizedBaseUrl, out error))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryNormalizeBaseUrl(string baseUrl, out string normalizedBaseUrl, out string error)
    {
        normalizedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://127.0.0.1:8080"
            : baseUrl.Trim().TrimEnd('/');
        error = "";

        if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "Base URL must be a full http:// or https:// URL.";
            return false;
        }

        return true;
    }

    private static async Task<bool> CanGetAsync(
        HttpClient httpClient,
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public record UserProviderRequest(
    string Provider,
    string ApiKey,
    string Model,
    string BaseUrl);

public record AdminProviderRequest(
    string Provider,
    string ApiKey,
    string Model,
    string BaseUrl);

public record ProviderTestRequest(
    string Provider,
    string BaseUrl,
    string Model);
