using LearnSphere.Models;

namespace LearnSphere.Services.Interfaces
{
    /// <summary>
    /// Course service interface - handles all course-related business logic.
    /// Separates business rules from data access and controllers.
    /// </summary>
    public interface ICourseService
    {
        // Course CRUD operations
        Task<Course?> GetCourseByIdAsync(int courseId);
        Task<IEnumerable<Course>> GetAllCoursesAsync();
        Task<IEnumerable<Course>> GetPublishedCoursesAsync();
        Task<IEnumerable<Course>> GetCoursesByInstructorAsync(string instructorId);
        Task<IEnumerable<Course>> GetCoursesByCategoryAsync(int categoryId);
        Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm);
        
        // Course management
        Task<Course> CreateCourseAsync(Course course, string instructorId);
        Task<bool> UpdateCourseAsync(Course course);
        Task<bool> DeleteCourseAsync(int courseId);
        
        // Course workflow
        Task<bool> SubmitCourseForReviewAsync(int courseId);
        Task<bool> ApproveCourseAsync(int courseId);
        Task<bool> RejectCourseAsync(int courseId, string reason);
        Task<bool> PublishCourseAsync(int courseId);
        Task<bool> ArchiveCourseAsync(int courseId);
        
        // Course details with related data
        Task<Course?> GetCourseWithLessonsAsync(int courseId);
        Task<Course?> GetCourseWithEnrollmentsAsync(int courseId);
        
        // Validation
        Task<bool> CanUserEditCourseAsync(int courseId, string userId);
        Task<bool> IsCourseAvailableForEnrollmentAsync(int courseId);
    }
}
