using LearnSphere.Models;

namespace LearnSphere.Repositories.Interfaces
{
    /// <summary>
    /// Certificate repository with specialized queries beyond basic CRUD.
    /// </summary>
    public interface ICertificateRepository : IRepository<Certificate>
    {
        Task<IEnumerable<Certificate>> GetByUserIdAsync(string userId);
        Task<Certificate?> GetByUserAndCourseAsync(string userId, int courseId);
        Task<Certificate?> GetByVerificationIdAsync(string verificationId);
    }
}
