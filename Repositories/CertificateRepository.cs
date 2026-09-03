using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnSphere.Repositories
{
    public class CertificateRepository : Repository<Certificate>, ICertificateRepository
    {
        public CertificateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Certificate>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(c => c.UserId == userId)
                .Include(c => c.Course)
                .OrderByDescending(c => c.IssuedDate)
                .ToListAsync();
        }

        public async Task<Certificate?> GetByUserAndCourseAsync(string userId, int courseId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);
        }

        public async Task<Certificate?> GetByVerificationIdAsync(string verificationId)
        {
            return await _dbSet
                .Include(c => c.User)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.VerificationId == verificationId);
        }
    }
}
