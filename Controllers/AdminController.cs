using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using LearnSphere.Repositories.Interfaces;
using LearnSphere.Services;
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
        private readonly IEmailSender _emailSender;

        public AdminController(IUnitOfWork unitOfWork, UserManager<User> userManager, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailSender = emailSender;
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

            await NotifyInstructorAsync(course, approved: true);

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

            await NotifyInstructorAsync(course, approved: false);

            TempData["Message"] = $"\"{course.Title}\" sent back to the instructor as a draft.";
            return RedirectToAction(nameof(Index));
        }

        private async Task NotifyInstructorAsync(Course course, bool approved)
        {
            if (!_emailSender.IsConfigured)
            {
                return;
            }

            var instructor = await _userManager.FindByIdAsync(course.InstructorId);

            if (instructor?.Email == null)
            {
                return;
            }

            var subject = approved
                ? $"\"{course.Title}\" was approved"
                : $"\"{course.Title}\" needs changes";
            var body = approved
                ? $"<p>Good news - your course <strong>{course.Title}</strong> was approved and is now live in the catalog.</p>"
                : $"<p>Your course <strong>{course.Title}</strong> was sent back to draft by an admin. Review it and submit again when it's ready.</p>";

            await _emailSender.SendEmailAsync(instructor.Email, subject, body);
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

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var allCourses = await _unitOfWork.Courses.GetAllAsync();

            var viewModels = categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CourseCount = allCourses.Count(course => course.CategoryId == c.Id)
                });

            return View(viewModels);
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new CategoryFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameTaken = (await _unitOfWork.Categories.GetAllAsync())
                .Any(c => c.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));

            if (nameTaken)
            {
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists.");
                return View(model);
            }

            await _unitOfWork.Categories.AddAsync(new Category
            {
                Name = model.Name,
                Description = model.Description
            });
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = $"Category \"{model.Name}\" created.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(model.Id ?? 0);

            if (category == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameTaken = (await _unitOfWork.Categories.GetAllAsync())
                .Any(c => c.Id != category.Id && c.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));

            if (nameTaken)
            {
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists.");
                return View(model);
            }

            category.Name = model.Name;
            category.Description = model.Description;
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = "Category updated.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            var hasCourses = (await _unitOfWork.Courses.GetAllAsync())
                .Any(c => c.CategoryId == id);

            if (hasCourses)
            {
                TempData["Error"] = $"\"{category.Name}\" still has courses assigned to it and can't be deleted.";
                return RedirectToAction(nameof(Categories));
            }

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            TempData["Message"] = $"Category \"{category.Name}\" deleted.";
            return RedirectToAction(nameof(Categories));
        }
    }
}
