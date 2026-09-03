using LearnSphere.Models;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Controllers
{
    public class CertificatesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public CertificatesController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var certificates = await _unitOfWork.Certificates.GetByUserIdAsync(userId);

            return View(certificates);
        }

        [HttpGet]
        public async Task<IActionResult> Verify(string? verificationId)
        {
            ViewBag.VerificationId = verificationId;

            if (string.IsNullOrWhiteSpace(verificationId))
            {
                return View();
            }

            var certificate = await _unitOfWork.Certificates.GetByVerificationIdAsync(verificationId.Trim());
            return View(certificate);
        }
    }
}
