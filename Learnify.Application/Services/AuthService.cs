using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Learnify.Application.Config;
using Learnify.Application.DTOs;
using Learnify.Application.Interfaces;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Learnify.Application.Services;

/// <summary>
/// Service implementation for authentication operations using BCrypt password hashing.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AuthResponseDTO>> RegisterAsync(RegisterDTO registerDTO)
    {
        var normalizedEmail = registerDTO.Email.ToLowerInvariant();
        var emailExists = await _unitOfWork.Users.EmailExistsAsync(normalizedEmail);
        if (emailExists)
        {
            return ApiResponse<AuthResponseDTO>.Fail(
                new List<string> { "An account with this email address already exists." });
        }

        if (registerDTO.Password != registerDTO.ConfirmPassword)
        {
            return ApiResponse<AuthResponseDTO>.Fail(
                new List<string> { "Passwords do not match." });
        }

        var user = new User
        {
            FullName = registerDTO.FullName,
            Email = normalizedEmail,
            Role = registerDTO.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            // BCrypt embeds salt inside the hash — no PasswordSalt needed
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        return ApiResponse<AuthResponseDTO>.Ok(
            new AuthResponseDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
            },
            "User registered successfully.");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO loginDTO)
    {
        var user = await _unitOfWork.Users.FindByEmailAsync(loginDTO.Email.ToLowerInvariant());
        if (user == null || !user.IsActive)
            return ApiResponse<AuthResponseDTO>.BadRequest("Invalid email or password.");

        // BCrypt.Verify compares the plain password against the stored hash
        if (!BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.PasswordHash))
            return ApiResponse<AuthResponseDTO>.BadRequest("Invalid email or password.");

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return ApiResponse<AuthResponseDTO>.Ok(
            new AuthResponseDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
            },
            "Login successful.");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AuthResponseDTO>> RefreshTokenAsync(string refreshToken, Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<AuthResponseDTO>.BadRequest("Invalid user.");

        var token = await GenerateJwtTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();

        return ApiResponse<AuthResponseDTO>.Ok(
            new AuthResponseDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Token = token,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
            },
            "Token refreshed successfully.");
    }

    /// <inheritdoc />
    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials);

        await Task.CompletedTask;
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
