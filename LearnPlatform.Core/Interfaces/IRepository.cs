using System.Linq.Expressions;

namespace LearnPlatform.Core.Interfaces;

/// <summary>
/// Generic repository interface defining common CRUD operations for any entity.
/// Implements the Repository Pattern for data access abstraction.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Returns all entities asynchronously.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Returns all entities asynchronously with tracking disabled for read-only scenarios.
    /// </summary>
    Task<IEnumerable<T>> GetAllNoTrackingAsync();

    /// <summary>
    /// Returns a single entity by its ID asynchronously.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    Task<T?> GetByIdAsync(object id);

    /// <summary>
    /// Returns entities matching the predicate asynchronously.
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns the first entity matching the predicate asynchronously.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns true if any entity matches the predicate asynchronously.
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns the count of entities matching the predicate asynchronously.
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Adds a new entity asynchronously.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Adds multiple entities asynchronously.
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Removes an entity asynchronously.
    /// </summary>
    void Remove(T entity);

    /// <summary>
    /// Removes multiple entities asynchronously.
    /// </summary>
    void RemoveRange(IEnumerable<T> entities);
}