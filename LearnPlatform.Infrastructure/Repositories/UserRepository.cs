using LearnPlatform.Core.Entities;
using LearnPlatform.Core.Interfaces;
using LearnPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPlatform.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of the User repository.
/// </summary>
public class UserRepository : EfRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> FindByRoleAsync(string role)
    {
        return await _dbSet.Where(u => u.Role == role).ToListAsync();
    }
}