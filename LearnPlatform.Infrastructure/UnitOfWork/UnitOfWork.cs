using LearnPlatform.Core.Interfaces;
using LearnPlatform.Infrastructure.Data;
using LearnPlatform.Infrastructure.Repositories;

namespace LearnPlatform.Infrastructure.UnitOfWork;

/// <summary>
/// Unit of Work implementation that coordinates repositories and manages database transactions.
/// Ensures all repository operations are committed together or rolled back together.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IUserRepository? _users;
    private ICourseRepository? _courses;
    private INoteRepository? _notes;
    private bool _disposed = false;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);

    public ICourseRepository Courses => _courses ??= new CourseRepository(_context);

    public INoteRepository Notes => _notes ??= new NoteRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public void RollbackTransaction()
    {
        _context.Database.RollbackTransactionAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        _disposed = true;
    }
}