using Learnify.Core.Entities;

namespace Learnify.Core.Interfaces;

public interface IUserAiSettingsRepository : IRepository<UserAiSettings>
{
    Task<UserAiSettings?> GetByUserIdAsync(Guid userId);
}
