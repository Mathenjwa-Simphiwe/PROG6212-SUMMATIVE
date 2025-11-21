using ContractClaimSystem.Data;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ContractClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly SessionService _sessionService;

        public AccountController(IDatabaseService databaseService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _sessionService = sessionService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect to dashboard
            if (_sessionService.IsUserLoggedIn())
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get all users and find matching credentials
                    var users = await _databaseService.GetUsersAsync();
                    var user = users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                    if (user != null)
                    {
                        _sessionService.SetUserSession(user);
                        TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";
                        return RedirectToAction("Dashboard", "Home");
                    }

                    ModelState.AddModelError("", "Invalid login attempt.");
                    TempData["ErrorMessage"] = "Invalid email or password.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred during login. Please try again.";
                    // Log the exception
                    Console.WriteLine($"Login error: {ex.Message}");
                }
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            _sessionService.ClearSession();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}