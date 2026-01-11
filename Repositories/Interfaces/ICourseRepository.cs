using LearnSphere.Models;

namespace LearnSphere.Repositories.Interfaces
{
    /// <summary>
    /// Course repository with specialized queries beyond basic CRUD.
    /// Inherits from IRepository for common operations.
    /// </summary>
    public interface ICourseRepository : IRepository<Course>
    {
        // Custom queries specific to courses
        Task<IEnumerable<Course>> GetPublishedCoursesAsync();
        Task<IEnumerable<Course>> GetCoursesByInstructorAsync(string instructorId);
        Task<IEnumerable<Course>> GetCoursesByCategoryAsync(int categoryId);
        Task<Course?> GetCourseWithLessonsAsync(int courseId);
        Task<Course?> GetCourseWithEnrollmentsAsync(int courseId);
        Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm);
    }
}