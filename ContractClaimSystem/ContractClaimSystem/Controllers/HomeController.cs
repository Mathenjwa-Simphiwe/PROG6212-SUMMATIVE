using System.Diagnostics;
using ContractClaimSystem.Data;
using ContractClaimSystem.Filters;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractClaimSystem.Controllers
{
    // Controllers/HomeController.cs
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SessionService _sessionService;

        public HomeController(ApplicationDbContext context, SessionService sessionService)
        {
            _context = context;
            _sessionService = sessionService;
        }

        [AuthorizeRole("Lecturer", "Coordinator", "Manager", "HR")]
        public IActionResult Dashboard()
        {
            var userRole = _sessionService.GetUserRole();
            ViewBag.UserRole = userRole;
            ViewBag.UserName = _sessionService.GetUserName();

            return View();
        }
    }
}
