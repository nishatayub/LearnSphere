using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace LearnSphere.Repositories
{
    /// <summary>
    /// Unit of Work implementation - coordinates repositories and manages transactions.
    /// Provides single point to save all changes atomically.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy initialization - repositories created only when needed
        private ICourseRepository? _courses;
        private IRepository<Category>? _categories;
        private IRepository<Enrollment>? _enrollments;
        private IRepository<Lesson>? _lessons;
        private IRepository<Progress>? _progressRecords;
        private IRepository<Certificate>? _certificates;
        private IRepository<CourseVersion>? _courseVersions;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lazy-loaded repository properties
        public ICourseRepository Courses => 
            _courses ??= new CourseRepository(_context);

        public IRepository<Category> Categories => 
            _categories ??= new Repository<Category>(_context);

        public IRepository<Enrollment> Enrollments => 
            _enrollments ??= new Repository<Enrollment>(_context);

        public IRepository<Lesson> Lessons => 
            _lessons ??= new Repository<Lesson>(_context);

        public IRepository<Progress> ProgressRecords => 
            _progressRecords ??= new Repository<Progress>(_context);

        public IRepository<Certificate> Certificates => 
            _certificates ??= new Repository<Certificate>(_context);

        public IRepository<CourseVersion> CourseVersions => 
            _courseVersions ??= new Repository<CourseVersion>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}