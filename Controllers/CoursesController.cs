using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Controllers
{
    public class CoursesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoursesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId)
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

            var viewModel = new CourseListViewModel
            {
                Courses = courses,
                Categories = await _unitOfWork.Categories.GetAllAsync(),
                SearchTerm = search,
                CategoryId = categoryId
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

            return View(course);
        }
    }
}
