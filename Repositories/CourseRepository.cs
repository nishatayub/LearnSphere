using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnSphere.Repositories
{
    /// <summary>
    /// Course repository with specialized query implementations.
    /// Extends generic repository with course-specific operations.
    /// </summary>
    public class CourseRepository : Repository<Course>, ICourseRepository
    {
        public CourseRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Course>> GetPublishedCoursesAsync()
        {
            return await _dbSet
                .Where(c => c.Status == CourseStatus.Published)
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> GetCoursesByInstructorAsync(string instructorId)
        {
            return await _dbSet
                .Where(c => c.InstructorId == instructorId)
                .Include(c => c.Category)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Course>> GetCoursesByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Where(c => c.CategoryId == categoryId && c.Status == CourseStatus.Published)
                .Include(c => c.Instructor)
                .OrderByDescending(c => c.TotalEnrollments)
                .ToListAsync();
        }

        public async Task<Course?> GetCourseWithLessonsAsync(int courseId)
        {
            return await _dbSet
                .Include(c => c.CurrentVersion)
                    .ThenInclude(v => v!.Lessons.OrderBy(l => l.OrderIndex))
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<Course?> GetCourseWithEnrollmentsAsync(int courseId)
        {
            return await _dbSet
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm)
        {
            return await _dbSet
                .Where(c => c.Status == CourseStatus.Published &&
                           (c.Title.Contains(searchTerm) ||
                            c.Description.Contains(searchTerm)))
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .ToListAsync();
        }
    }
}