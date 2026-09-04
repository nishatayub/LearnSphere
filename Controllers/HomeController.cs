using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using LearnSphere.Repositories.Interfaces;

namespace LearnSphere.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var publishedCourses = (await _unitOfWork.Courses.GetPublishedCoursesAsync()).ToList();

        var viewModel = new HomeIndexViewModel
        {
            Categories = await _unitOfWork.Categories.GetAllAsync(),
            FeaturedCourses = publishedCourses.Take(6),
            TotalPublishedCourses = publishedCourses.Count
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
