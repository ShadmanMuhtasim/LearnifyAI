using Learnify.Application.Settings;
using Microsoft.Extensions.Options;

namespace Learnify.Infrastructure.AI;

/// <summary>
/// Singleton. Holds:
///   1. Admin default provider config (loaded from user-secrets, NEVER sent to frontend)
///   2. Per-user provider configs (in-memory Dictionary, isolated per userId)
///   3. Rate limit buckets for users on the default key (20 requests/hour)
/// Thread-safe via ReaderWriterLockSlim.
/// </summary>
public class UserAiSettingsStore
{
    // ── Internal types ────────────────────────────────────────────────────────

    public record ProviderConfig(
        string Provider,   // "Gemini" | "OpenAI" | "Claude" | "Ollama" | "LocalOpenAI" | "Mock"
        string ApiKey,     // empty for Ollama/Mock and optional for LocalOpenAI
        string Model,      // free text — whatever the user/admin types
        string BaseUrl     // only for Ollama
    );

    private record RateBucket(int Count, DateTime WindowStart);

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly ReaderWriterLockSlim _lock = new();

    // Admin default — loaded from user-secrets at startup, changeable via admin endpoint
    private ProviderConfig _adminDefault;

    // Per-user overrides — keyed by userId (Guid as string)
    private readonly Dictionary<string, ProviderConfig> _userConfigs = new();

    // Rate limit buckets — only tracked for users using the admin default key
    private readonly Dictionary<string, RateBucket> _rateBuckets = new();

    private const int RequestsPerHour = 20;

    // ── Constructor ───────────────────────────────────────────────────────────

    public UserAiSettingsStore(IOptions<AiSettings> options)
    {
        var s = options.Value;
        _adminDefault = new ProviderConfig(
            Provider: s.ActiveProvider,
            ApiKey:   GetAdminKey(s),
            Model:    GetAdminModel(s),
            BaseUrl:  GetAdminBaseUrl(s)
        );
    }

    private static string GetAdminKey(AiSettings s) =>
        s.ActiveProvider.ToLowerInvariant() switch
        {
            "gemini" => s.Gemini?.ApiKey ?? "",
            "openai" => s.OpenAi?.ApiKey ?? "",
            "claude" => s.Claude?.ApiKey ?? "",
            "localopenai" => s.LocalOpenAI?.ApiKey ?? "",
            _ => ""
        };

    private static string GetAdminModel(AiSettings s) =>
        s.ActiveProvider.ToLowerInvariant() switch
        {
            "gemini" => s.Gemini?.Model ?? "",
            "openai" => s.OpenAi?.Model ?? "",
            "claude" => s.Claude?.Model ?? "",
            "ollama" => s.Ollama?.Model ?? "",
            "localopenai" => s.LocalOpenAI?.Model ?? "",
            _ => ""
        };

    private static string GetAdminBaseUrl(AiSettings s) =>
        s.ActiveProvider.ToLowerInvariant() switch
        {
            "ollama" => s.Ollama?.BaseUrl ?? "",
            "localopenai" => s.LocalOpenAI?.BaseUrl ?? "",
            _ => ""
        };

    // ── Admin operations ──────────────────────────────────────────────────────

    /// <summary>Admin sets the global default. Key is stored only in memory.</summary>
    public void SetAdminDefault(string provider, string apiKey, string model, string baseUrl)
    {
        _lock.EnterWriteLock();
        try { _adminDefault = new(provider, apiKey, model, baseUrl); }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Returns admin default provider NAME only — never the key.</summary>
    public string GetAdminDefaultProviderName()
    {
        _lock.EnterReadLock();
        try { return _adminDefault.Provider; }
        finally { _lock.ExitReadLock(); }
    }

    // ── User operations ───────────────────────────────────────────────────────

    /// <summary>User saves their own provider config (their own API key, isolated).</summary>
    public void SetUserConfig(Guid userId, string provider, string apiKey, string model, string baseUrl)
    {
        _lock.EnterWriteLock();
        try { _userConfigs[userId.ToString()] = new(provider, apiKey, model, baseUrl); }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>User reverts to the admin default.</summary>
    public void ClearUserConfig(Guid userId)
    {
        _lock.EnterWriteLock();
        try { _userConfigs.Remove(userId.ToString()); }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Resolves which config to use for this request.
    /// Returns (config, isUsingDefault) so the caller knows whether to rate-limit.
    /// </summary>
    public (ProviderConfig Config, bool IsUsingDefault) Resolve(Guid userId)
    {
        _lock.EnterReadLock();
        try
        {
            if (_userConfigs.TryGetValue(userId.ToString(), out var userCfg))
                return (userCfg, false);
            return (_adminDefault, true);
        }
        finally { _lock.ExitReadLock(); }
    }

    // ── Rate limiting ─────────────────────────────────────────────────────────

    /// <summary>
    /// Checks and records a request for a user on the default key.
    /// Returns true if allowed, false if rate limit exceeded.
    /// </summary>
    public bool TryConsumeDefaultKeyRequest(Guid userId)
    {
        var key = userId.ToString();
        _lock.EnterWriteLock();
        try
        {
            if (_rateBuckets.TryGetValue(key, out var bucket))
            {
                // Reset bucket if the window has expired
                if ((DateTime.UtcNow - bucket.WindowStart).TotalHours >= 1)
                    bucket = new RateBucket(0, DateTime.UtcNow);

                if (bucket.Count >= RequestsPerHour)
                    return false;

                _rateBuckets[key] = bucket with { Count = bucket.Count + 1 };
            }
            else
            {
                _rateBuckets[key] = new RateBucket(1, DateTime.UtcNow);
            }
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Returns remaining requests in current window for the default key.</summary>
    public int GetRemainingRequests(Guid userId)
    {
        var key = userId.ToString();
        _lock.EnterReadLock();
        try
        {
            if (!_rateBuckets.TryGetValue(key, out var bucket)) return RequestsPerHour;
            if ((DateTime.UtcNow - bucket.WindowStart).TotalHours >= 1) return RequestsPerHour;
            return Math.Max(0, RequestsPerHour - bucket.Count);
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Returns user-facing status (NEVER includes the admin API key).
    /// </summary>
    public object GetUserStatus(Guid userId)
    {
        _lock.EnterReadLock();
        try
        {
            var hasOwn = _userConfigs.TryGetValue(userId.ToString(), out var own);
            return new
            {
                mode = hasOwn ? "own" : "default",
                provider = hasOwn ? own!.Provider : _adminDefault.Provider,
                model = hasOwn ? own!.Model : _adminDefault.Model,
                baseUrl = hasOwn
                    ? own!.BaseUrl
                    : IsLocalProvider(_adminDefault.Provider)
                        ? _adminDefault.BaseUrl
                        : "",
                remainingDefaultRequests = GetRemainingRequestsNoLock(userId)
                // NOTE: ApiKey is intentionally NEVER included here
            };
        }
        finally { _lock.ExitReadLock(); }
    }

    private static bool IsLocalProvider(string provider) =>
        provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("LocalOpenAI", StringComparison.OrdinalIgnoreCase);

    private int GetRemainingRequestsNoLock(Guid userId)
    {
        var key = userId.ToString();
        if (!_rateBuckets.TryGetValue(key, out var bucket)) return RequestsPerHour;
        if ((DateTime.UtcNow - bucket.WindowStart).TotalHours >= 1) return RequestsPerHour;
        return Math.Max(0, RequestsPerHour - bucket.Count);
    }
}
