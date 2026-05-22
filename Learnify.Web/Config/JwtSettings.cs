namespace Learnify.Web.Config;

/// <summary>
/// JWT configuration settings from appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryHours { get; set; } = 7;
}