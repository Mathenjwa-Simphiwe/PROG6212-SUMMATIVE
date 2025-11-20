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

        public IActionResult GenerateReport()
        {
            var claims = _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == "Approved")
                .ToList();

            // Generate PDF report (you would implement PDF generation here)
            var reportData = claims.Select(c => new
            {
                Lecturer = $"{c.Lecturer.FirstName} {c.Lecturer.LastName}",
                Period = $"{c.Month}/{c.Year}",
                Hours = c.HoursWorked,
                Amount = c.TotalAmount
            }).ToList();

            ViewBag.ReportData = reportData;
            return View();
        }
    }
}
