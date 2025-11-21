using ContractClaimSystem.Data;
using ContractClaimSystem.Filters;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ContractClaimSystem.Controllers
{
  
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SessionService _sessionService;
        private readonly IWebHostEnvironment _environment;

        public ClaimsController(ApplicationDbContext context, SessionService sessionService, IWebHostEnvironment environment)
        {
            _context = context;
            _sessionService = sessionService;
            _environment = environment;
        }

        // GET: Submit Claim Form
        [AuthorizeRole("Lecturer")]
        [HttpGet]
        public IActionResult SubmitClaim()
        {
            var lecturer = _context.Users.Find(_sessionService.GetUserId());
            ViewBag.HourlyRate = lecturer?.HourlyRate ?? 0;
            ViewBag.MaxHours = 180; // Maximum hours allowed
            return View(new Claim { Month = DateTime.Now.Month, Year = DateTime.Now.Year });
        }

        // POST: Submit Claim
        [AuthorizeRole("Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile supportingDocument)
        {
            try
            {
                // Server-side validation
                if (claim.HoursWorked > 180)
                {
                    ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month.");
                }

                if (claim.HoursWorked <= 0)
                {
                    ModelState.AddModelError("HoursWorked", "Hours worked must be greater than 0.");
                }

                if (ModelState.IsValid)
                {
                    var lecturer = await _context.Users.FindAsync(_sessionService.GetUserId());
                    if (lecturer == null)
                    {
                        TempData["ErrorMessage"] = "Lecturer not found.";
                        return RedirectToAction("SubmitClaim");
                    }

                    // Create new claim object to avoid model binding issues
                    var newClaim = new Claim
                    {
                        LecturerId = _sessionService.GetUserId(),
                        Month = claim.Month,
                        Year = claim.Year,
                        HoursWorked = claim.HoursWorked,
                        HourlyRate = lecturer.HourlyRate,
                        TotalAmount = claim.HoursWorked * lecturer.HourlyRate,
                        Notes = claim.Notes,
                        Status = "Pending",
                        SubmittedDate = DateTime.Now
                    };

                    // Handle file upload
                    if (supportingDocument != null && supportingDocument.Length > 0)
                    {
                        await HandleFileUpload(newClaim, supportingDocument);
                    }

                    _context.Claims.Add(newClaim);

                    // Add initial status history
                    newClaim.StatusHistory.Add(new ClaimStatusHistory
                    {
                        Status = "Pending",
                        ActionBy = _sessionService.GetUserName(),
                        Notes = "Claim submitted",
                        ActionDate = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Claim submitted successfully!";
                    return RedirectToAction("MyClaims");
                }

                // If we get here, there were validation errors
                var currentLecturer = await _context.Users.FindAsync(_sessionService.GetUserId());
                ViewBag.HourlyRate = currentLecturer?.HourlyRate ?? 0;
                ViewBag.MaxHours = 180;
                return View(claim);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error submitting claim: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while submitting your claim. Please try again.";

                var currentLecturer = await _context.Users.FindAsync(_sessionService.GetUserId());
                ViewBag.HourlyRate = currentLecturer?.HourlyRate ?? 0;
                ViewBag.MaxHours = 180;
                return View(claim);
            }
        }

        private async Task HandleFileUpload(Claim claim, IFormFile file)
        {
            // Validate file size (5MB limit)
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new Exception("File size must be less than 5MB.");
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Only PDF, DOCX, XLSX, JPG, and PNG files are allowed.");
            }

            // Store file in database
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                claim.FileName = file.FileName;
                claim.FileContent = memoryStream.ToArray();
                claim.ContentType = file.ContentType;
            }

        }

            // Download supporting document
            [AuthorizeRole("Lecturer", "Coordinator", "Manager", "HR")]
        public IActionResult DownloadDocument(int id)
        {
            var claim = _context.Claims.Find(id);
            if (claim?.FileContent == null)
            {
                return NotFound();
            }

            // Check authorization
            var userRole = _sessionService.GetUserRole();
            var userId = _sessionService.GetUserId();

            if (userRole == "Lecturer" && claim.LecturerId != userId)
            {
                return Forbid();
            }

            return File(claim.FileContent, claim.ContentType ?? "application/octet-stream", claim.FileName);
        }

        // View Claims with Status Tracking
        [AuthorizeRole("Lecturer")]
        public IActionResult MyClaims()
        {
            var claims = _context.Claims
                .Include(c => c.StatusHistory)
                .Where(c => c.LecturerId == _sessionService.GetUserId())
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            return View(claims);
        }

        // Coordinator: View Pending Claims
        [AuthorizeRole("Coordinator")]
        public IActionResult PendingClaims()
        {
            var claims = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.StatusHistory)
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.SubmittedDate)
                .ToList();

            return View(claims);
        }

        // Coordinator: Approve Claim
        [AuthorizeRole("Coordinator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveClaim(int id, string notes = "")
        {
            try
            {
                var claim = _context.Claims
                    .Include(c => c.StatusHistory)
                    .FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    return NotFound();
                }

                claim.Status = "ApprovedByCoordinator";
                claim.CoordinatorApprovedDate = DateTime.Now;

                claim.StatusHistory.Add(new ClaimStatusHistory
                {
                    Status = "ApprovedByCoordinator",
                    ActionBy = _sessionService.GetUserName(),
                    Notes = notes ?? "Approved by Coordinator",
                    ActionDate = DateTime.Now
                });

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Claim approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
            }

            return RedirectToAction("PendingClaims");
        }

        // Coordinator: Reject Claim
        [AuthorizeRole("Coordinator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectClaim(int id, string notes)
        {
            try
            {
                var claim = _context.Claims
                    .Include(c => c.StatusHistory)
                    .FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    return NotFound();
                }

                if (string.IsNullOrEmpty(notes))
                {
                    TempData["ErrorMessage"] = "Rejection notes are required.";
                    return RedirectToAction("PendingClaims");
                }

                claim.Status = "Rejected";

                claim.StatusHistory.Add(new ClaimStatusHistory
                {
                    Status = "Rejected",
                    ActionBy = _sessionService.GetUserName(),
                    Notes = notes,
                    ActionDate = DateTime.Now
                });

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
            }

            return RedirectToAction("PendingClaims");
        }

        // Manager: View Claims for Final Approval
        [AuthorizeRole("Manager")]
        public IActionResult ManagerApproval()
        {
            var claims = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.StatusHistory)
                .Where(c => c.Status == "ApprovedByCoordinator")
                .OrderBy(c => c.CoordinatorApprovedDate)
                .ToList();

            return View(claims);
        }

        // Manager: Final Approve Claim
        [AuthorizeRole("Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalApproveClaim(int id, string notes = "")
        {
            try
            {
                var claim = _context.Claims
                    .Include(c => c.StatusHistory)
                    .FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    return NotFound();
                }

                claim.Status = "Approved";
                claim.ManagerApprovedDate = DateTime.Now;

                claim.StatusHistory.Add(new ClaimStatusHistory
                {
                    Status = "Approved",
                    ActionBy = _sessionService.GetUserName(),
                    Notes = notes ?? "Approved by Academic Manager",
                    ActionDate = DateTime.Now
                });

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Claim approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
            }

            return RedirectToAction("ManagerApproval");
        }

        // Manager: Final Reject Claim
        [AuthorizeRole("Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinalRejectClaim(int id, string notes)
        {
            try
            {
                var claim = _context.Claims
                    .Include(c => c.StatusHistory)
                    .FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    return NotFound();
                }

                if (string.IsNullOrEmpty(notes))
                {
                    TempData["ErrorMessage"] = "Rejection notes are required.";
                    return RedirectToAction("ManagerApproval");
                }

                claim.Status = "Rejected";

                claim.StatusHistory.Add(new ClaimStatusHistory
                {
                    Status = "Rejected",
                    ActionBy = _sessionService.GetUserName(),
                    Notes = notes,
                    ActionDate = DateTime.Now
                });

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
            }

            return RedirectToAction("ManagerApproval");
        }

        // Claim Details with Status History
        [AuthorizeRole("Lecturer", "Coordinator", "Manager", "HR")]
        public IActionResult ClaimDetails(int id)
        {
            var claim = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.StatusHistory)
                .FirstOrDefault(c => c.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            // Authorization check
            var userRole = _sessionService.GetUserRole();
            var userId = _sessionService.GetUserId();

            if (userRole == "Lecturer" && claim.LecturerId != userId)
            {
                return Forbid();
            }

            return View(claim);
        }
    }
}
