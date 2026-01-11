namespace LearnSphere.Repositories.Interfaces
{
    /// <summary>
    /// Generic repository interface for basic CRUD operations.
    /// T is the entity type (User, Course, etc.)
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        // READ operations
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
        
        // CREATE
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        
        // UPDATE
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        
        // DELETE
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        
        // UTILITY
        Task<bool> ExistsAsync(int id);
        Task<int> CountAsync();
    }
}