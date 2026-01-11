using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using LearnSphere.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnSphere.Services
{
    /// <summary>
    /// Enrollment service implementation - manages enrollments, progress tracking, and certificates.
    /// Implements business rules for course enrollment and completion workflows.
    /// </summary>
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(IUnitOfWork unitOfWork, ILogger<EnrollmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Enrollment?> EnrollStudentAsync(string studentId, int courseId)
        {
            try
            {
                // Business rule: Check if student is already enrolled
                var existingEnrollment = await GetEnrollmentAsync(studentId, courseId);
                if (existingEnrollment != null)
                {
                    _logger.LogWarning("Student {StudentId} already enrolled in course {CourseId}", studentId, courseId);
                    return null;
                }

                // Business rule: Check if course is available for enrollment
                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null || course.Status != CourseStatus.Published)
                {
                    _logger.LogWarning("Course {CourseId} not available for enrollment", courseId);
                    return null;
                }

                // Get the active course version
                var courseVersions = await _unitOfWork.CourseVersions.FindAsync(cv => 
                    cv.CourseId == courseId && cv.IsActive);
                var activeCourseVersion = courseVersions.FirstOrDefault();
                
                if (activeCourseVersion == null)
                {
                    _logger.LogWarning("No active version for course {CourseId}", courseId);
                    return null;
                }

                // Create enrollment
                var enrollment = new Enrollment
                {
                    UserId = studentId,
                    CourseId = courseId,
                    CourseVersionId = activeCourseVersion.Id,
                    EnrolledDate = DateTime.UtcNow,
                    ProgressPercentage = 0,
                    Status = EnrollmentStatus.Active
                };

                await _unitOfWork.Enrollments.AddAsync(enrollment);

                // Update course enrollment count
                course.TotalEnrollments++;
                _unitOfWork.Courses.Update(course);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} enrolled in course {CourseId}", studentId, courseId);
                return enrollment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}", studentId, courseId);
                return null;
            }
        }

        public async Task<bool> UnenrollStudentAsync(string studentId, int courseId)
        {
            try
            {
                var enrollment = await GetEnrollmentAsync(studentId, courseId);
                if (enrollment == null)
                    return false;

                // Business rule: Cannot unenroll from completed courses
                if (enrollment.Status == EnrollmentStatus.Completed)
                {
                    _logger.LogWarning("Cannot unenroll from completed course {CourseId}", courseId);
                    return false;
                }

                _unitOfWork.Enrollments.Remove(enrollment);

                // Update course enrollment count
                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course != null)
                {
                    course.TotalEnrollments--;
                    _unitOfWork.Courses.Update(course);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} unenrolled from course {CourseId}", studentId, courseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling student {StudentId} from course {CourseId}", studentId, courseId);
                return false;
            }
        }

        public async Task<Enrollment?> GetEnrollmentAsync(string studentId, int courseId)
        {
            var enrollments = await _unitOfWork.Enrollments.FindAsync(e => 
                e.UserId == studentId && e.CourseId == courseId);
            return enrollments.FirstOrDefault();
        }

        public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(string studentId)
        {
            return await _unitOfWork.Enrollments.FindAsync(e => e.UserId == studentId);
        }

        public async Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId)
        {
            return await _unitOfWork.Enrollments.FindAsync(e => e.CourseId == courseId);
        }

        public async Task<bool> UpdateLessonProgressAsync(string studentId, int lessonId, bool isCompleted)
        {
            try
            {
                // Get the lesson to find course and enrollment
                var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
                if (lesson == null)
                    return false;

                // Get enrollment
                var enrollment = await GetEnrollmentAsync(studentId, lesson.CourseVersionId);
                if (enrollment == null)
                    return false;

                // Check if progress record exists
                var existingProgress = await GetLessonProgressForEnrollmentAsync(enrollment.Id, lessonId);

                if (existingProgress == null)
                {
                    // Create new progress record
                    var progress = new Progress
                    {
                        EnrollmentId = enrollment.Id,
                        LessonId = lessonId,
                        IsCompleted = isCompleted,
                        CompletedDate = isCompleted ? DateTime.UtcNow : null
                    };

                    await _unitOfWork.ProgressRecords.AddAsync(progress);
                }
                else
                {
                    // Update existing progress
                    existingProgress.IsCompleted = isCompleted;
                    existingProgress.CompletedDate = isCompleted ? DateTime.UtcNow : null;
                    _unitOfWork.ProgressRecords.Update(existingProgress);
                }

                await _unitOfWork.SaveChangesAsync();

                // Recalculate course progress
                await RecalculateCourseProgressAsync(studentId, lesson.CourseVersionId);

                _logger.LogInformation("Progress updated for student {StudentId}, lesson {LessonId}: {IsCompleted}", 
                    studentId, lessonId, isCompleted);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for student {StudentId}, lesson {LessonId}", studentId, lessonId);
                return false;
            }
        }

        public async Task<Progress?> GetLessonProgressAsync(string studentId, int lessonId)
        {
            // Get lesson to find course
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
            if (lesson == null)
                return null;

            var enrollment = await GetEnrollmentAsync(studentId, lesson.CourseVersionId);
            if (enrollment == null)
                return null;

            return await GetLessonProgressForEnrollmentAsync(enrollment.Id, lessonId);
        }

        private async Task<Progress?> GetLessonProgressForEnrollmentAsync(int enrollmentId, int lessonId)
        {
            var progresses = await _unitOfWork.ProgressRecords.FindAsync(p => 
                p.EnrollmentId == enrollmentId && p.LessonId == lessonId);
            return progresses.FirstOrDefault();
        }

        public async Task<IEnumerable<Progress>> GetCourseProgressAsync(string studentId, int courseId)
        {
            // Get enrollment
            var enrollment = await GetEnrollmentAsync(studentId, courseId);
            if (enrollment == null)
                return new List<Progress>();

            // Get all lessons for the course version
            var lessons = await _unitOfWork.Lessons.FindAsync(l => l.CourseVersionId == enrollment.CourseVersionId);
            var lessonIds = lessons.Select(l => l.Id).ToList();

            // Get progress for those lessons
            var allProgress = await _unitOfWork.ProgressRecords.FindAsync(p => 
                p.EnrollmentId == enrollment.Id && lessonIds.Contains(p.LessonId));

            return allProgress;
        }

        public async Task<decimal> CalculateCourseCompletionAsync(string studentId, int courseId)
        {
            var enrollment = await GetEnrollmentAsync(studentId, courseId);
            if (enrollment == null)
                return 0;

            var lessons = await _unitOfWork.Lessons.FindAsync(l => l.CourseVersionId == enrollment.CourseVersionId);
            var totalLessons = lessons.Count();

            if (totalLessons == 0)
                return 0;

            var progress = await GetCourseProgressAsync(studentId, courseId);
            var completedLessons = progress.Count(p => p.IsCompleted);

            return Math.Round((decimal)completedLessons / totalLessons * 100, 2);
        }

        public async Task<bool> CompleteCourseAsync(string studentId, int courseId)
        {
            try
            {
                var enrollment = await GetEnrollmentAsync(studentId, courseId);
                if (enrollment == null)
                    return false;

                // Business rule: Check if all lessons are completed
                var completionPercentage = await CalculateCourseCompletionAsync(studentId, courseId);
                if (completionPercentage < 100)
                {
                    _logger.LogWarning("Cannot complete course {CourseId} - only {Percentage}% complete", 
                        courseId, completionPercentage);
                    return false;
                }

                enrollment.Status = EnrollmentStatus.Completed;
                enrollment.CompletedDate = DateTime.UtcNow;
                enrollment.ProgressPercentage = 100;

                _unitOfWork.Enrollments.Update(enrollment);
                await _unitOfWork.SaveChangesAsync();

                // Generate certificate
                await GenerateCertificateAsync(studentId, courseId);

                _logger.LogInformation("Student {StudentId} completed course {CourseId}", studentId, courseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing course {CourseId} for student {StudentId}", courseId, studentId);
                return false;
            }
        }

        public async Task<Certificate?> GenerateCertificateAsync(string studentId, int courseId)
        {
            try
            {
                // Business rule: Check if course is completed
                var enrollment = await GetEnrollmentAsync(studentId, courseId);
                if (enrollment == null || enrollment.Status != EnrollmentStatus.Completed)
                {
                    _logger.LogWarning("Cannot generate certificate - course not completed");
                    return null;
                }

                // Check if certificate already exists
                var existingCertificates = await _unitOfWork.Certificates.FindAsync(c => 
                    c.UserId == studentId && c.CourseId == courseId);
                if (existingCertificates.Any())
                {
                    _logger.LogInformation("Certificate already exists for student {StudentId}, course {CourseId}", 
                        studentId, courseId);
                    return existingCertificates.First();
                }

                // Generate unique verification ID
                var verificationId = $"LS-{courseId:D6}-{studentId.GetHashCode():X8}-{DateTime.UtcNow:yyyyMMdd}";

                var certificate = new Certificate
                {
                    UserId = studentId,
                    CourseId = courseId,
                    IssuedDate = DateTime.UtcNow,
                    VerificationId = verificationId
                };

                await _unitOfWork.Certificates.AddAsync(certificate);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Certificate {VerificationId} generated for student {StudentId}", 
                    verificationId, studentId);
                return certificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for student {StudentId}, course {CourseId}", 
                    studentId, courseId);
                return null;
            }
        }

        public async Task<IEnumerable<Certificate>> GetStudentCertificatesAsync(string studentId)
        {
            return await _unitOfWork.Certificates.FindAsync(c => c.UserId == studentId);
        }

        public async Task<bool> IsStudentEnrolledAsync(string studentId, int courseId)
        {
            var enrollment = await GetEnrollmentAsync(studentId, courseId);
            return enrollment != null;
        }

        public async Task<bool> CanEnrollInCourseAsync(string studentId, int courseId)
        {
            // Check if already enrolled
            if (await IsStudentEnrolledAsync(studentId, courseId))
                return false;

            // Check if course is published
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            return course != null && course.Status == CourseStatus.Published;
        }

        public async Task<bool> HasCompletedCourseAsync(string studentId, int courseId)
        {
            var enrollment = await GetEnrollmentAsync(studentId, courseId);
            return enrollment?.Status == EnrollmentStatus.Completed;
        }

        // Helper method to recalculate course progress
        private async Task RecalculateCourseProgressAsync(string studentId, int courseVersionId)
        {
            // Find enrollment for this course version
            var enrollments = await _unitOfWork.Enrollments.FindAsync(e => 
                e.UserId == studentId && e.CourseVersionId == courseVersionId);
            var enrollment = enrollments.FirstOrDefault();
            
            if (enrollment == null)
                return;

            var lessons = await _unitOfWork.Lessons.FindAsync(l => l.CourseVersionId == courseVersionId);
            var totalLessons = lessons.Count();

            if (totalLessons == 0)
                return;

            var progresses = await _unitOfWork.ProgressRecords.FindAsync(p => p.EnrollmentId == enrollment.Id);
            var completedLessons = progresses.Count(p => p.IsCompleted);

            enrollment.ProgressPercentage = Math.Round((decimal)completedLessons / totalLessons * 100, 2);

            _unitOfWork.Enrollments.Update(enrollment);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
