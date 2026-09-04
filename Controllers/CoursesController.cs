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

            if (lesson.ContentType == ContentType.Quiz)
            {
                viewModel.QuizQuestions = (await _unitOfWork.QuizQuestions
                    .FindAsync(q => q.LessonId == lessonId))
                    .OrderBy(q => q.OrderIndex)
                    .ToList();

                var questionIds = viewModel.QuizQuestions.Select(q => q.Id).ToHashSet();
                var allOptions = await _unitOfWork.QuizOptions
                    .FindAsync(o => questionIds.Contains(o.QuizQuestionId));
                var optionsByQuestion = allOptions.ToLookup(o => o.QuizQuestionId);

                foreach (var question in viewModel.QuizQuestions)
                {
                    question.Options = optionsByQuestion[question.Id].ToList();
                }

                viewModel.LatestQuizAttempt = (await _unitOfWork.QuizAttempts
                    .FindAsync(a => a.EnrollmentId == enrollment.Id && a.LessonId == lessonId))
                    .OrderByDescending(a => a.AttemptedDate)
                    .FirstOrDefault();
            }

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int courseId, int lessonId, Dictionary<int, int> answers)
        {
            var userId = _userManager.GetUserId(User)!;
            var enrollment = await _unitOfWork.Enrollments.GetByUserAndCourseAsync(userId, courseId);

            if (enrollment == null)
            {
                return NotFound();
            }

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);

            if (lesson == null || lesson.CourseVersionId != enrollment.CourseVersionId || lesson.ContentType != ContentType.Quiz)
            {
                return NotFound();
            }

            var questions = await _unitOfWork.QuizQuestions.FindAsync(q => q.LessonId == lessonId);
            var questionIds = questions.Select(q => q.Id).ToHashSet();
            var options = await _unitOfWork.QuizOptions.FindAsync(o => questionIds.Contains(o.QuizQuestionId));
            var correctOptionIdByQuestion = options
                .Where(o => o.IsCorrect)
                .ToDictionary(o => o.QuizQuestionId, o => o.Id);

            var totalQuestions = questionIds.Count;
            var correctAnswers = questionIds.Count(questionId =>
                answers.TryGetValue(questionId, out var selectedOptionId) &&
                correctOptionIdByQuestion.TryGetValue(questionId, out var correctOptionId) &&
                selectedOptionId == correctOptionId);

            var scorePercentage = totalQuestions == 0
                ? 0
                : Math.Round(100m * correctAnswers / totalQuestions, 2);

            var attempt = new QuizAttempt
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                ScorePercentage = scorePercentage,
                Passed = scorePercentage >= 70
            };
            await _unitOfWork.QuizAttempts.AddAsync(attempt);
            await _unitOfWork.SaveChangesAsync();

            if (attempt.Passed)
            {
                await MarkLessonCompletedAsync(enrollment, lessonId);
                await RecomputeEnrollmentProgressAsync(enrollment, userId, courseId);
            }

            return RedirectToAction(nameof(Lesson), new { courseId, lessonId });
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

            if (lesson.ContentType == ContentType.Quiz)
            {
                // Quiz lessons are only completed by passing SubmitQuiz - block the
                // manual toggle so it can't be used to bypass grading via a direct POST.
                return BadRequest("Quiz lessons are completed by passing the quiz, not by marking them complete manually.");
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

            await RecomputeEnrollmentProgressAsync(enrollment, userId, courseId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Learn), new { id = courseId });
        }

        /// <summary>
        /// Marks a lesson complete without the toggle-off behavior ToggleLesson has -
        /// used by the quiz pass path, where re-submitting a passing attempt should
        /// never accidentally un-complete a lesson.
        /// </summary>
        private async Task MarkLessonCompletedAsync(Enrollment enrollment, int lessonId)
        {
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
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedDate = DateTime.UtcNow;
                _unitOfWork.ProgressRecords.Update(progress);
            }

            progress.LastAccessedDate = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Recomputes an enrollment's progress percentage/status from the Progress
        /// table and issues a certificate at 100% - shared by ToggleLesson and
        /// SubmitQuiz so both paths recompute the exact same way.
        /// </summary>
        private async Task RecomputeEnrollmentProgressAsync(Enrollment enrollment, string userId, int courseId)
        {
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
