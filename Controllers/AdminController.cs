using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public AdminController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
            var publishedCourses = await _unitOfWork.Courses.GetCoursesByStatusAsync(CourseStatus.Published);
            var pendingCourses = await _unitOfWork.Courses.GetCoursesByStatusAsync(CourseStatus.UnderReview);
            var allCourses = await _unitOfWork.Courses.GetAllAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = _userManager.Users.Count(),
                TotalStudents = students.Count,
                TotalInstructors = instructors.Count,
                TotalCourses = allCourses.Count(),
                PublishedCourses = publishedCourses.Count(),
                PendingReviewCourses = pendingCourses.Count(),
                CoursesPendingReview = pendingCourses
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var course = await _unitOfWork.Courses.GetCourseWithLessonsAsync(id);

            if (course == null || course.Status != CourseStatus.UnderReview)
            {
                return NotFound();
            }

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(id);

            if (course == null || course.Status != CourseStatus.UnderReview)
            {
                return NotFound();
            }

            course.Status = CourseStatus.Published;
            course.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = $"\"{course.Title}\" approved and published.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(id);

            if (course == null || course.Status != CourseStatus.UnderReview)
            {
                return NotFound();
            }

            course.Status = CourseStatus.Draft;
            course.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = $"\"{course.Title}\" sent back to the instructor as a draft.";
            return RedirectToAction(nameof(Index));
        }
    }
}
