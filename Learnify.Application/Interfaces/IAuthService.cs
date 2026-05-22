using Learnify.Application.DTOs;
using Learnify.Core.Entities;

namespace Learnify.Application.Interfaces;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user and returns authentication tokens.
    /// </summary>
    Task<ApiResponse<AuthResponseDTO>> RegisterAsync(RegisterDTO registerDTO);

    /// <summary>
    /// Authenticates a user and returns authentication tokens.
    /// </summary>
    Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO loginDTO);

    /// <summary>
    /// Refreshes an expired JWT token using a valid refresh token.
    /// </summary>
    Task<ApiResponse<AuthResponseDTO>> RefreshTokenAsync(string refreshToken, Guid userId);

    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    Task<string> GenerateJwtTokenAsync(User user);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    string GenerateRefreshToken();
}
