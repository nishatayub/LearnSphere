using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public InstructorController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var instructorId = _userManager.GetUserId(User)!;
            var courses = await _unitOfWork.Courses.GetCoursesByInstructorAsync(instructorId);

            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            var model = new CourseFormViewModel
            {
                Categories = await _unitOfWork.Categories.GetAllAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CourseFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _unitOfWork.Categories.GetAllAsync();
                return View(model);
            }

            var instructorId = _userManager.GetUserId(User)!;

            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                ThumbnailUrl = model.ThumbnailUrl,
                CategoryId = model.CategoryId,
                Difficulty = model.Difficulty,
                EstimatedDurationHours = model.EstimatedDurationHours,
                InstructorId = instructorId,
                Status = CourseStatus.Draft
            };

            await _unitOfWork.Courses.AddAsync(course);
            await _unitOfWork.SaveChangesAsync();

            var version = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Changelog = "Initial version"
            };

            await _unitOfWork.CourseVersions.AddAsync(version);
            await _unitOfWork.SaveChangesAsync();

            course.CurrentVersionId = version.Id;
            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Course created as a draft. Add some lessons, then publish it when you're ready.";
            return RedirectToAction(nameof(Lessons), new { courseId = course.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await GetOwnedCourseAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            var model = new CourseFormViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                CategoryId = course.CategoryId,
                Difficulty = course.Difficulty,
                EstimatedDurationHours = course.EstimatedDurationHours,
                Categories = await _unitOfWork.Categories.GetAllAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(CourseFormViewModel model)
        {
            var course = await GetOwnedCourseAsync(model.Id ?? 0);

            if (course == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _unitOfWork.Categories.GetAllAsync();
                return View(model);
            }

            course.Title = model.Title;
            course.Description = model.Description;
            course.ThumbnailUrl = model.ThumbnailUrl;
            course.CategoryId = model.CategoryId;
            course.Difficulty = model.Difficulty;
            course.EstimatedDurationHours = model.EstimatedDurationHours;
            course.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Course details updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null || course.Status != CourseStatus.Draft)
            {
                return NotFound();
            }

            var hasLessons = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == course.CurrentVersionId))
                .Any();

            if (!hasLessons)
            {
                TempData["Error"] = "Add at least one lesson before submitting for review.";
                return RedirectToAction(nameof(Lessons), new { courseId });
            }

            course.Status = CourseStatus.UnderReview;
            course.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Submitted for review. An admin will publish it once approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> NewVersion(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null || course.Status != CourseStatus.Published)
            {
                return NotFound();
            }

            ViewBag.Course = course;
            return View(new NewVersionViewModel { CourseId = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewVersion(NewVersionViewModel model)
        {
            var course = await GetOwnedCourseAsync(model.CourseId);

            if (course == null || course.Status != CourseStatus.Published)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Course = course;
                return View(model);
            }

            var currentVersion = await _unitOfWork.CourseVersions.GetByIdAsync(course.CurrentVersionId!.Value);
            var currentLessons = await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == course.CurrentVersionId);

            var newVersion = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = (currentVersion?.VersionNumber ?? 0) + 1,
                Changelog = model.Changelog
            };

            await _unitOfWork.CourseVersions.AddAsync(newVersion);
            await _unitOfWork.SaveChangesAsync();

            // Existing enrollments keep pointing at the old CourseVersionId, so students
            // already partway through the course aren't disrupted by this update.
            foreach (var lesson in currentLessons)
            {
                await _unitOfWork.Lessons.AddAsync(new Lesson
                {
                    CourseVersionId = newVersion.Id,
                    Title = lesson.Title,
                    Description = lesson.Description,
                    ContentType = lesson.ContentType,
                    Content = lesson.Content,
                    ContentUrl = lesson.ContentUrl,
                    OrderIndex = lesson.OrderIndex,
                    DurationMinutes = lesson.DurationMinutes,
                    IsFree = lesson.IsFree
                });
            }

            course.CurrentVersionId = newVersion.Id;
            course.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Courses.Update(course);

            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = $"Version {newVersion.VersionNumber} created. Edit its lessons below - students already enrolled keep their original content.";
            return RedirectToAction(nameof(Lessons), new { courseId = course.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Enrollments(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var courseWithEnrollments = await _unitOfWork.Courses.GetCourseWithEnrollmentsAsync(courseId);

            ViewBag.Course = course;
            return View(courseWithEnrollments!.Enrollments.OrderByDescending(e => e.EnrolledDate));
        }

        [HttpGet]
        public async Task<IActionResult> Analytics(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var courseWithEnrollments = await _unitOfWork.Courses.GetCourseWithEnrollmentsAsync(courseId);
            var enrollments = courseWithEnrollments!.Enrollments.ToList();

            var lessons = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == course.CurrentVersionId))
                .OrderBy(l => l.OrderIndex)
                .ToList();

            var enrollmentIds = enrollments.Select(e => e.Id).ToHashSet();
            var completedProgress = (await _unitOfWork.ProgressRecords
                .FindAsync(p => p.IsCompleted && enrollmentIds.Contains(p.EnrollmentId)))
                .ToList();

            var lessonStats = lessons.Select(lesson =>
            {
                var completedCount = completedProgress.Count(p => p.LessonId == lesson.Id);
                return new LessonStatItem
                {
                    Lesson = lesson,
                    CompletedCount = completedCount,
                    CompletionRate = enrollments.Count == 0
                        ? 0
                        : Math.Round(100m * completedCount / enrollments.Count, 1)
                };
            });

            var viewModel = new CourseAnalyticsViewModel
            {
                Course = course,
                TotalEnrollments = enrollments.Count,
                ActiveCount = enrollments.Count(e => e.Status == EnrollmentStatus.Active),
                CompletedCount = enrollments.Count(e => e.Status == EnrollmentStatus.Completed),
                DroppedCount = enrollments.Count(e => e.Status is EnrollmentStatus.Dropped or EnrollmentStatus.Suspended),
                AverageProgress = enrollments.Count == 0
                    ? 0
                    : Math.Round(enrollments.Average(e => e.ProgressPercentage), 1),
                LessonStats = lessonStats
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Lessons(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var lessons = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == course.CurrentVersionId))
                .OrderBy(l => l.OrderIndex);

            var currentVersion = await _unitOfWork.CourseVersions.GetByIdAsync(course.CurrentVersionId!.Value);

            ViewBag.Course = course;
            ViewBag.VersionNumber = currentVersion?.VersionNumber;
            return View(lessons);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLesson(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var nextOrder = (await _unitOfWork.Lessons
                .FindAsync(l => l.CourseVersionId == course.CurrentVersionId))
                .Select(l => (int?)l.OrderIndex)
                .Max() ?? 0;

            ViewBag.Course = course;
            return View(new LessonFormViewModel { CourseId = courseId, OrderIndex = nextOrder + 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(LessonFormViewModel model)
        {
            var course = await GetOwnedCourseAsync(model.CourseId);

            if (course == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Course = course;
                return View(model);
            }

            var lesson = new Lesson
            {
                CourseVersionId = course.CurrentVersionId!.Value,
                Title = model.Title,
                Description = model.Description,
                ContentType = model.ContentType,
                Content = model.Content,
                ContentUrl = model.ContentUrl,
                OrderIndex = model.OrderIndex,
                DurationMinutes = model.DurationMinutes,
                IsFree = model.IsFree
            };

            await _unitOfWork.Lessons.AddAsync(lesson);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Lesson added.";
            return RedirectToAction(nameof(Lessons), new { courseId = model.CourseId });
        }

        [HttpGet]
        public async Task<IActionResult> EditLesson(int courseId, int lessonId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);

            if (lesson == null || lesson.CourseVersionId != course.CurrentVersionId)
            {
                return NotFound();
            }

            ViewBag.Course = course;
            return View(new LessonFormViewModel
            {
                Id = lesson.Id,
                CourseId = courseId,
                Title = lesson.Title,
                Description = lesson.Description,
                ContentType = lesson.ContentType,
                Content = lesson.Content,
                ContentUrl = lesson.ContentUrl,
                OrderIndex = lesson.OrderIndex,
                DurationMinutes = lesson.DurationMinutes,
                IsFree = lesson.IsFree
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(LessonFormViewModel model)
        {
            var course = await GetOwnedCourseAsync(model.CourseId);

            if (course == null)
            {
                return NotFound();
            }

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(model.Id ?? 0);

            if (lesson == null || lesson.CourseVersionId != course.CurrentVersionId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Course = course;
                return View(model);
            }

            lesson.Title = model.Title;
            lesson.Description = model.Description;
            lesson.ContentType = model.ContentType;
            lesson.Content = model.Content;
            lesson.ContentUrl = model.ContentUrl;
            lesson.OrderIndex = model.OrderIndex;
            lesson.DurationMinutes = model.DurationMinutes;
            lesson.IsFree = model.IsFree;

            _unitOfWork.Lessons.Update(lesson);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Lesson updated.";
            return RedirectToAction(nameof(Lessons), new { courseId = model.CourseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int courseId, int lessonId)
        {
            var course = await GetOwnedCourseAsync(courseId);

            if (course == null)
            {
                return NotFound();
            }

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);

            if (lesson == null || lesson.CourseVersionId != course.CurrentVersionId)
            {
                return NotFound();
            }

            var hasStudentProgress = (await _unitOfWork.ProgressRecords
                .FindAsync(p => p.LessonId == lessonId))
                .Any();

            if (hasStudentProgress)
            {
                TempData["Error"] = "This lesson can't be removed because students have already made progress on it.";
                return RedirectToAction(nameof(Lessons), new { courseId });
            }

            _unitOfWork.Lessons.Remove(lesson);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Lesson removed.";
            return RedirectToAction(nameof(Lessons), new { courseId });
        }

        private async Task<Course?> GetOwnedCourseAsync(int courseId)
        {
            var instructorId = _userManager.GetUserId(User)!;
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);

            return course != null && course.InstructorId == instructorId ? course : null;
        }
    }
}
