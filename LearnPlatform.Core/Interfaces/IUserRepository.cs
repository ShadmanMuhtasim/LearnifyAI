using LearnPlatform.Core.Entities;

namespace LearnPlatform.Core.Interfaces;

/// <summary>
/// User-specific repository interface extending the generic repository.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    Task<User?> FindByEmailAsync(string email);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Finds users by role asynchronously.
    /// </summary>
    Task<IEnumerable<User>> FindByRoleAsync(string role);
}