using LearnSphere.Models;
using LearnSphere.Services.Interfaces;
using LearnSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace LearnSphere.Controllers
{
    /// <summary>
    /// Course controller - handles course CRUD operations and browsing.
    /// Separates instructor actions (create, edit) from student actions (browse, enroll).
    /// </summary>
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            ICourseService courseService,
            IEnrollmentService enrollmentService,
            ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _enrollmentService = enrollmentService;
            _logger = logger;
        }

        // GET: /Course/Index - Browse all published courses
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? categoryId, string? searchTerm)
        {
            IEnumerable<Course> courses;

            if (categoryId.HasValue)
            {
                courses = await _courseService.GetCoursesByCategoryAsync(categoryId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                courses = await _courseService.SearchCoursesAsync(searchTerm);
            }
            else
            {
                courses = await _courseService.GetPublishedCoursesAsync();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CategoryId = categoryId;
            return View(courses);
        }

        // GET: /Course/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _courseService.GetCourseWithLessonsAsync(id);
            if (course == null)
                return NotFound();

            // Check if user is enrolled (if authenticated)
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    ViewBag.IsEnrolled = await _enrollmentService.IsStudentEnrolledAsync(userId, id);
                }
            }

            return View(course);
        }

        // GET: /Course/Create - Instructor only
        [Authorize(Roles = "Instructor,Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> Create(CreateCourseViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Difficulty = model.DifficultyLevel,
                ThumbnailUrl = model.ThumbnailUrl
            };

            var createdCourse = await _courseService.CreateCourseAsync(course, userId);

            _logger.LogInformation("Course {CourseId} created by {UserId}", createdCourse.Id, userId);

            TempData["Success"] = "Course created successfully!";
            return RedirectToAction(nameof(MyCourses));
        }

        // GET: /Course/MyCourses - Instructor's courses
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> MyCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var courses = await _courseService.GetCoursesByInstructorAsync(userId);
            return View(courses);
        }

        // GET: /Course/Edit/5
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Check if user can edit this course
            if (!await _courseService.CanUserEditCourseAsync(id, userId))
                return Forbid();

            return View(course);
        }

        // POST: /Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(course);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Check if user can edit this course
            if (!await _courseService.CanUserEditCourseAsync(id, userId))
                return Forbid();

            var success = await _courseService.UpdateCourseAsync(course);

            if (success)
            {
                TempData["Success"] = "Course updated successfully!";
                return RedirectToAction(nameof(MyCourses));
            }

            ModelState.AddModelError(string.Empty, "Failed to update course");
            return View(course);
        }

        // POST: /Course/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Check if user can edit this course
            if (!await _courseService.CanUserEditCourseAsync(id, userId))
                return Forbid();

            var success = await _courseService.DeleteCourseAsync(id);

            if (success)
            {
                TempData["Success"] = "Course deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete course. Only draft courses can be deleted.";
            }

            return RedirectToAction(nameof(MyCourses));
        }

        // POST: /Course/Publish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> Publish(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            if (!await _courseService.CanUserEditCourseAsync(id, userId))
                return Forbid();

            var success = await _courseService.PublishCourseAsync(id);

            if (success)
            {
                TempData["Success"] = "Course published successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to publish course.";
            }

            return RedirectToAction(nameof(MyCourses));
        }

        // POST: /Course/Enroll/5 - Student enrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var enrollment = await _enrollmentService.EnrollStudentAsync(userId, id);

            if (enrollment != null)
            {
                TempData["Success"] = "Successfully enrolled in course!";
                return RedirectToAction(nameof(MyEnrollments));
            }

            TempData["Error"] = "Failed to enroll in course.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Course/MyEnrollments - Student's enrolled courses
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyEnrollments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
            return View(enrollments);
        }
    }
}
