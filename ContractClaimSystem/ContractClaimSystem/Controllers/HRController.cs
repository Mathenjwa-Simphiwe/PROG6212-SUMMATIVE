using ContractClaimSystem.Data;
using ContractClaimSystem.Filters;
using ContractClaimSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractClaimSystem.Controllers
{
    // Controllers/HRController.cs
    [AuthorizeRole("HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                user.CreatedDate = DateTime.Now;
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Update(user);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        public IActionResult GenerateReport(int? month, int? year, string status)
        {
            try
            {
                // Start with base query
                var claimsQuery = _context.Claims
                    .Include(c => c.Lecturer)
                    .AsQueryable();

                // Apply filters safely
                if (month.HasValue && month > 0)
                    claimsQuery = claimsQuery.Where(c => c.Month == month.Value);

                if (year.HasValue && year > 0)
                    claimsQuery = claimsQuery.Where(c => c.Year == year.Value);

                if (!string.IsNullOrEmpty(status))
                    claimsQuery = claimsQuery.Where(c => c.Status == status);

                // Execute query and handle null results
                var claims = claimsQuery
                    .OrderByDescending(c => c.SubmittedDate)
                    .ToList() ?? new List<Claim>(); // Ensure we never return null

                return View(claims);
            }
            catch (Exception ex)
            {
                // Log the error and return empty list
                Console.WriteLine($"Error generating report: {ex.Message}");
                return View(new List<Claim>());
            }
        }

    }
}
