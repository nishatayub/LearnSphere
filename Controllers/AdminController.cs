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
        private static readonly string[] AssignableRoles = { "Student", "Instructor", "Admin" };

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

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.OrderBy(u => u.Email).ToList();
            var viewModels = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                viewModels.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email!,
                    Role = roles.FirstOrDefault() ?? "(no role)",
                    CreatedAt = user.CreatedAt,
                    IsLockedOut = await _userManager.IsLockedOutAsync(user)
                });
            }

            return View(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            if (userId == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You can't change your own role.";
                return RedirectToAction(nameof(Users));
            }

            if (!AssignableRoles.Contains(role))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["Message"] = $"{user.Email} is now a{(role == "Admin" ? "n" : "")} {role}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            if (userId == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You can't lock your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, isLockedOut ? null : DateTimeOffset.MaxValue);

            TempData["Message"] = isLockedOut ? $"{user.Email} unlocked." : $"{user.Email} locked out.";
            return RedirectToAction(nameof(Users));
        }
    }
}
