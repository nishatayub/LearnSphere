using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnSphere.Repositories
{
    public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Enrollment>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .OrderByDescending(e => e.EnrolledDate)
                .ToListAsync();
        }

        public async Task<Enrollment?> GetByUserAndCourseAsync(string userId, int courseId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        }
    }
}
