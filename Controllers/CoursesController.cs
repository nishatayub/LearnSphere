using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Controllers
{
    public class CoursesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public CoursesController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            DifficultyLevel? difficulty,
            CourseSortOrder sort = CourseSortOrder.Newest,
            int page = 1)
        {
            var courses = await _unitOfWork.Courses.GetPublishedCoursesAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                courses = courses.Where(c =>
                    c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (categoryId.HasValue)
            {
                courses = courses.Where(c => c.CategoryId == categoryId.Value);
            }

            if (difficulty.HasValue)
            {
                courses = courses.Where(c => c.Difficulty == difficulty.Value);
            }

            courses = sort switch
            {
                CourseSortOrder.TitleAZ => courses.OrderBy(c => c.Title),
                CourseSortOrder.MostEnrolled => courses.OrderByDescending(c => c.TotalEnrollments),
                _ => courses.OrderByDescending(c => c.CreatedAt)
            };

            var totalCount = courses.Count();
            const int pageSize = 6;
            page = Math.Max(page, 1);
            var pagedCourses = courses.Skip((page - 1) * pageSize).Take(pageSize);

            var viewModel = new CourseListViewModel
            {
                Courses = pagedCourses,
                Categories = await _unitOfWork.Categories.GetAllAsync(),
                SearchTerm = search,
                CategoryId = categoryId,
                Difficulty = difficulty,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _unitOfWork.Courses.GetCourseWithLessonsAsync(id);

            if (course == null || course.Status != CourseStatus.Published)
            {
                return NotFound();
            }

            if (_userManager.GetUserId(User) is { } userId)
            {
                var enrollment = await _unitOfWork.Enrollments.GetByUserAndCourseAsync(userId, id);
                ViewBag.IsEnrolled = enrollment != null;
            }

            return View(course);
        }

        [Authorize]
        public async Task<IActionResult> Learn(int id)
        {
            var course = await _unitOfWork.Courses.GetCourseWithLessonsAsync(id);

            if (course == null || course.Status != CourseStatus.Published)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            var enrollment = await _unitOfWork.Enrollments.GetByUserAndCourseAsync(userId, id);

            if (enrollment == null)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            // Always pull lessons from the version the student enrolled under, not the
            // course's current version - those can diverge once an instructor publishes
            // a new version, and enrolled students shouldn't see content shift under them.
            var lessons = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == enrollment.CourseVersionId))
                .OrderBy(l => l.OrderIndex);
            var progressRecords = await _unitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == enrollment.Id);
            var completedLessonIds = progressRecords.Where(p => p.IsCompleted).Select(p => p.LessonId).ToHashSet();

            var viewModel = new LearnViewModel
            {
                Course = course,
                Enrollment = enrollment,
                Lessons = lessons.Select(l => new LessonProgressItem
                {
                    Lesson = l,
                    IsCompleted = completedLessonIds.Contains(l.Id)
                })
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Lesson(int courseId, int lessonId)
        {
            var course = await _unitOfWork.Courses.GetCourseWithLessonsAsync(courseId);

            if (course == null || course.Status != CourseStatus.Published)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            var enrollment = await _unitOfWork.Enrollments.GetByUserAndCourseAsync(userId, courseId);

            if (enrollment == null)
            {
                return RedirectToAction(nameof(Details), new { id = courseId });
            }

            var orderedLessons = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == enrollment.CourseVersionId))
                .OrderBy(l => l.OrderIndex)
                .ToList();
            var lesson = orderedLessons.FirstOrDefault(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound();
            }

            var isCompleted = (await _unitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == enrollment.Id && p.LessonId == lessonId))
                .Any(p => p.IsCompleted);

            var currentIndex = orderedLessons.FindIndex(l => l.Id == lessonId);

            var viewModel = new LessonViewModel
            {
                Course = course,
                Lesson = lesson,
                IsCompleted = isCompleted,
                PreviousLessonId = currentIndex > 0 ? orderedLessons[currentIndex - 1].Id : null,
                NextLessonId = currentIndex < orderedLessons.Count - 1 ? orderedLessons[currentIndex + 1].Id : null
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLesson(int courseId, int lessonId, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User)!;
            var enrollment = await _unitOfWork.Enrollments.GetByUserAndCourseAsync(userId, courseId);

            if (enrollment == null)
            {
                return NotFound();
            }

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);

            if (lesson == null || lesson.CourseVersionId != enrollment.CourseVersionId)
            {
                return NotFound();
            }

            var progress = (await _unitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == enrollment.Id && p.LessonId == lessonId))
                .FirstOrDefault();

            if (progress == null)
            {
                progress = new Progress
                {
                    EnrollmentId = enrollment.Id,
                    LessonId = lessonId,
                    IsCompleted = true,
                    CompletedDate = DateTime.UtcNow
                };
                await _unitOfWork.ProgressRecords.AddAsync(progress);
            }
            else
            {
                progress.IsCompleted = !progress.IsCompleted;
                progress.CompletedDate = progress.IsCompleted ? DateTime.UtcNow : null;
                _unitOfWork.ProgressRecords.Update(progress);
            }

            progress.LastAccessedDate = DateTime.UtcNow;

            // Persist the progress change first - the completed-count query below
            // hits the database directly, so an unsaved add/update wouldn't be counted.
            await _unitOfWork.SaveChangesAsync();

            var totalLessons = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == enrollment.CourseVersionId))
                .Count();
            var completedLessons = (await _unitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == enrollment.Id && p.IsCompleted))
                .Count();

            enrollment.ProgressPercentage = totalLessons == 0
                ? 0
                : Math.Round(100m * completedLessons / totalLessons, 2);
            enrollment.LastAccessedDate = DateTime.UtcNow;

            if (enrollment.ProgressPercentage == 100)
            {
                enrollment.Status = EnrollmentStatus.Completed;
                enrollment.CompletedDate ??= DateTime.UtcNow;
                await IssueCertificateIfNeededAsync(userId, courseId);
            }
            else if (enrollment.Status == EnrollmentStatus.Completed)
            {
                enrollment.Status = EnrollmentStatus.Active;
                enrollment.CompletedDate = null;
            }

            _unitOfWork.Enrollments.Update(enrollment);
            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Learn), new { id = courseId });
        }

        private async Task IssueCertificateIfNeededAsync(string userId, int courseId)
        {
            var existing = await _unitOfWork.Certificates.GetByUserAndCourseAsync(userId, courseId);

            if (existing != null)
            {
                return;
            }

            var certificate = new Certificate
            {
                UserId = userId,
                CourseId = courseId,
                VerificationId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()
            };

            await _unitOfWork.Certificates.AddAsync(certificate);
        }
    }
}
