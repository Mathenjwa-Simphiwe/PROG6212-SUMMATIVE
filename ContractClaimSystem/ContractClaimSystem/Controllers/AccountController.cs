using ContractClaimSystem.Data;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SessionService _sessionService;

        public AccountController(ApplicationDbContext context, SessionService sessionService)
        {
            _context = context;
            _sessionService = sessionService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null)
                {
                    _sessionService.SetUserSession(user);
                    return RedirectToAction("Dashboard", "Home");
                }
                ModelState.AddModelError("", "Invalid login attempt.");
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            _sessionService.ClearSession();
            return RedirectToAction("Login");
        }
    }
}
