using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Controllers
{
    [Authorize]
    public class EnrollmentsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public EnrollmentsController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var enrollments = await _unitOfWork.Enrollments.GetByUserIdAsync(userId);

            return View(enrollments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);

            if (course == null || course.Status != CourseStatus.Published || course.CurrentVersionId == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            var existingEnrollment = await _unitOfWork.Enrollments.GetByUserAndCourseAsync(userId, courseId);

            if (existingEnrollment == null)
            {
                var enrollment = new Enrollment
                {
                    UserId = userId,
                    CourseId = courseId,
                    CourseVersionId = course.CurrentVersionId.Value
                };

                await _unitOfWork.Enrollments.AddAsync(enrollment);

                course.TotalEnrollments++;
                _unitOfWork.Courses.Update(course);

                await _unitOfWork.SaveChangesAsync();
            }

            TempData["Message"] = $"You're enrolled in {course.Title}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
