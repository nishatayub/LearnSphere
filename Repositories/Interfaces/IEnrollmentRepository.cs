using LearnSphere.Models;

namespace LearnSphere.Repositories.Interfaces
{
    /// <summary>
    /// Enrollment repository with specialized queries beyond basic CRUD.
    /// </summary>
    public interface IEnrollmentRepository : IRepository<Enrollment>
    {
        Task<IEnumerable<Enrollment>> GetByUserIdAsync(string userId);
        Task<Enrollment?> GetByUserAndCourseAsync(string userId, int courseId);
    }
}
