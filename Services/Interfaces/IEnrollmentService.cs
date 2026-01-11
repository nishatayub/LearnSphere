using LearnSphere.Models;

namespace LearnSphere.Services.Interfaces
{
    /// <summary>
    /// Enrollment service interface - manages student course enrollments and progress tracking.
    /// Handles business logic for enrollment rules, progress calculations, and certificates.
    /// </summary>
    public interface IEnrollmentService
    {
        // Enrollment operations
        Task<Enrollment?> EnrollStudentAsync(string studentId, int courseId);
        Task<bool> UnenrollStudentAsync(string studentId, int courseId);
        Task<Enrollment?> GetEnrollmentAsync(string studentId, int courseId);
        Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(string studentId);
        Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId);
        
        // Progress tracking
        Task<bool> UpdateLessonProgressAsync(string studentId, int lessonId, bool isCompleted);
        Task<Progress?> GetLessonProgressAsync(string studentId, int lessonId);
        Task<IEnumerable<Progress>> GetCourseProgressAsync(string studentId, int courseId);
        Task<decimal> CalculateCourseCompletionAsync(string studentId, int courseId);
        
        // Course completion & certificates
        Task<bool> CompleteCourseAsync(string studentId, int courseId);
        Task<Certificate?> GenerateCertificateAsync(string studentId, int courseId);
        Task<IEnumerable<Certificate>> GetStudentCertificatesAsync(string studentId);
        
        // Validation
        Task<bool> IsStudentEnrolledAsync(string studentId, int courseId);
        Task<bool> CanEnrollInCourseAsync(string studentId, int courseId);
        Task<bool> HasCompletedCourseAsync(string studentId, int courseId);
    }
}
