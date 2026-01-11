using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using LearnSphere.Services.Interfaces;

namespace LearnSphere.Services
{
    /// <summary>
    /// Course service implementation - business logic for course management.
    /// Uses Unit of Work pattern for data access and transaction management.
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CourseService> _logger;

        public CourseService(IUnitOfWork unitOfWork, ILogger<CourseService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Course?> GetCourseByIdAsync(int courseId)
        {
            return await _unitOfWork.Courses.GetByIdAsync(courseId);
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            return await _unitOfWork.Courses.GetAllAsync();
        }

        public async Task<IEnumerable<Course>> GetPublishedCoursesAsync()
        {
            return await _unitOfWork.Courses.GetPublishedCoursesAsync();
        }

        public async Task<IEnumerable<Course>> GetCoursesByInstructorAsync(string instructorId)
        {
            return await _unitOfWork.Courses.GetCoursesByInstructorAsync(instructorId);
        }

        public async Task<IEnumerable<Course>> GetCoursesByCategoryAsync(int categoryId)
        {
            return await _unitOfWork.Courses.GetCoursesByCategoryAsync(categoryId);
        }

        public async Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetPublishedCoursesAsync();

            return await _unitOfWork.Courses.SearchCoursesAsync(searchTerm);
        }

        public async Task<Course> CreateCourseAsync(Course course, string instructorId)
        {
            // Business rule: Set instructor and initial status
            course.InstructorId = instructorId;
            course.Status = CourseStatus.Draft;
            course.CreatedAt = DateTime.UtcNow;
            course.TotalEnrollments = 0;

            await _unitOfWork.Courses.AddAsync(course);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course {CourseId} created by instructor {InstructorId}", course.Id, instructorId);
            return course;
        }

        public async Task<bool> UpdateCourseAsync(Course course)
        {
            try
            {
                course.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Courses.Update(course);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Course {CourseId} updated successfully", course.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", course.Id);
                return false;
            }
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            try
            {
                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null)
                    return false;

                // Business rule: Can only delete draft courses
                if (course.Status != CourseStatus.Draft)
                {
                    _logger.LogWarning("Attempted to delete non-draft course {CourseId}", courseId);
                    return false;
                }

                _unitOfWork.Courses.Remove(course);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Course {CourseId} deleted", courseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                return false;
            }
        }

        public async Task<bool> SubmitCourseForReviewAsync(int courseId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null || course.Status != CourseStatus.Draft)
                return false;

            course.Status = CourseStatus.UnderReview;
            course.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course {CourseId} submitted for review", courseId);
            return true;
        }

        public async Task<bool> ApproveCourseAsync(int courseId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null || course.Status != CourseStatus.UnderReview)
                return false;

            course.Status = CourseStatus.Published;
            course.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course {CourseId} approved and published", courseId);
            return true;
        }

        public async Task<bool> RejectCourseAsync(int courseId, string reason)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null || course.Status != CourseStatus.UnderReview)
                return false;

            course.Status = CourseStatus.Draft;
            course.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course {CourseId} rejected. Reason: {Reason}", courseId, reason);
            return true;
        }

        public async Task<bool> PublishCourseAsync(int courseId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            course.Status = CourseStatus.Published;
            course.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course {CourseId} published", courseId);
            return true;
        }

        public async Task<bool> ArchiveCourseAsync(int courseId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            course.Status = CourseStatus.Archived;
            course.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Course {CourseId} archived", courseId);
            return true;
        }

        public async Task<Course?> GetCourseWithLessonsAsync(int courseId)
        {
            return await _unitOfWork.Courses.GetCourseWithLessonsAsync(courseId);
        }

        public async Task<Course?> GetCourseWithEnrollmentsAsync(int courseId)
        {
            return await _unitOfWork.Courses.GetCourseWithEnrollmentsAsync(courseId);
        }

        public async Task<bool> CanUserEditCourseAsync(int courseId, string userId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            // Business rule: Only instructor who created the course can edit it
            return course.InstructorId == userId;
        }

        public async Task<bool> IsCourseAvailableForEnrollmentAsync(int courseId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            // Business rule: Only published courses can be enrolled in
            return course.Status == CourseStatus.Published;
        }
    }
}
