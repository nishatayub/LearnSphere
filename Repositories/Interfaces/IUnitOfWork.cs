using LearnSphere.Models;

namespace LearnSphere.Repositories.Interfaces
{
    /// <summary>
    /// Unit of Work pattern - manages transactions and coordinates multiple repositories.
    /// Ensures all changes are saved together or rolled back together.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties - access to all repositories
        ICourseRepository Courses { get; }
        IRepository<Category> Categories { get; }
        IRepository<Enrollment> Enrollments { get; }
        IRepository<Lesson> Lessons { get; }
        IRepository<Progress> ProgressRecords { get; }
        IRepository<Certificate> Certificates { get; }
        IRepository<CourseVersion> CourseVersions { get; }
        
        // Transaction management
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}