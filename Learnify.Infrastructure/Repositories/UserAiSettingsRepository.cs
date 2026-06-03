using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnify.Infrastructure.Repositories;

public class UserAiSettingsRepository : EfRepository<UserAiSettings>, IUserAiSettingsRepository
{
    public UserAiSettingsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<UserAiSettings?> GetByUserIdAsync(Guid userId)
    {
        return _dbSet.FirstOrDefaultAsync(settings => settings.UserId == userId);
    }
}
