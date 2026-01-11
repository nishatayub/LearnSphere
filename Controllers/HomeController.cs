using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LearnSphere.Models;
using LearnSphere.Services.Interfaces;

namespace LearnSphere.Controllers;

/// <summary>
/// Home controller - handles landing page and public pages.
/// Shows published courses to all visitors.
/// </summary>
public class HomeController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ICourseService courseService, ILogger<HomeController> logger)
    {
        _courseService = courseService;
        _logger = logger;
    }

    // GET: /Home/Index
    public async Task<IActionResult> Index()
    {
        var publishedCourses = await _courseService.GetPublishedCoursesAsync();
        return View(publishedCourses);
    }

    // GET: /Home/Privacy
    public IActionResult Privacy()
    {
        return View();
    }

    // GET: /Home/Error
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
