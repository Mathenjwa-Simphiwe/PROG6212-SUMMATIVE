using ContractClaimSystem.Filters;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;
using ContractClaimSystem.Data;
using Microsoft.AspNetCore.Authorization;

namespace ContractClaimSystem.Controllers
{
    [AuthorizeRole("HR")]
    public class HRController : Controller
    {
        private readonly IDatabaseService _databaseService;

        public HRController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _databaseService.GetUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var success = await _databaseService.AddUserAsync(user);

                    if (success)
                    {
                        TempData["SuccessMessage"] = $"User {user.FirstName} {user.LastName} created successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create user. The email may already exist.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating user: {ex.Message}";
            }

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _databaseService.GetUserAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var success = await _databaseService.UpdateUserAsync(user);

                    if (success)
                    {
                        TempData["SuccessMessage"] = $"User {user.FirstName} {user.LastName} updated successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update user. The user may not exist or the email may already be in use.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating user: {ex.Message}";
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var success = await _databaseService.DeleteUserAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "User deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user. The user may not exist or cannot be deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> GenerateReport(int? month, int? year, string status)
        {
            try
            {
                var claims = await _databaseService.GetClaimsReportAsync(month, year, status);
                return View(claims);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                return View(new List<Claim>());
            }
        }
    }
}