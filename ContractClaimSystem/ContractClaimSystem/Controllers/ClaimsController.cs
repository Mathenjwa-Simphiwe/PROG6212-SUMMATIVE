using ContractClaimSystem.Data;
using ContractClaimSystem.Filters;
using ContractClaimSystem.Models;
using ContractClaimSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ContractClaimSystem.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly SessionService _sessionService;

        public ClaimsController(IDatabaseService databaseService, SessionService sessionService)
        {
            _databaseService = databaseService;
            _sessionService = sessionService;
        }

        // GET: Submit Claim Form
        [AuthorizeRole("Lecturer")]
        [HttpGet]
        public async Task<IActionResult> SubmitClaim()
        {
            var lecturer = await _databaseService.GetUserAsync(_sessionService.GetUserId());
            ViewBag.HourlyRate = lecturer?.HourlyRate ?? 0;
            ViewBag.MaxHours = 180;

            // Initialize with default values
            var claim = new Claim
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                FileName = "",
                ContentType = "",
                FileContent = new byte[0]
            };

            return View(claim);
        }

        // POST: Submit Claim
        [AuthorizeRole("Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile supportingDocument)
        {
            Console.WriteLine($"=== SUBMIT CLAIM STARTED ===");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            var userId = _sessionService.GetUserId();
            var userName = _sessionService.GetUserName();
            var userRole = _sessionService.GetUserRole();

            Console.WriteLine($"Session - UserId: {userId}, UserName: {userName}, Role: {userRole}");
            Console.WriteLine($"Claim data - Month: {claim.Month}, Year: {claim.Year}, Hours: {claim.HoursWorked}");

            // Remove errors for system-populated fields
            ModelState.Remove("FileName");
            ModelState.Remove("Lecturer");
            ModelState.Remove("ContentType");
            ModelState.Remove("FileContent");
            ModelState.Remove("HourlyRate");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("Status");
            ModelState.Remove("SubmittedDate");

            Console.WriteLine($"ModelState.IsValid after removing system fields: {ModelState.IsValid}");

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

                // Log all ModelState errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }

                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is valid - calling SubmitClaimAsync...");

                    var success = await _databaseService.SubmitClaimAsync(
                        claim,
                        supportingDocument,
                        userId,
                        userName
                    );

                    Console.WriteLine($"SubmitClaimAsync result: {success}");

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Claim submitted successfully!";
                        return RedirectToAction("MyClaims");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to submit claim. Please try again.";
                        Console.WriteLine("SubmitClaimAsync returned false");
                    }
                }
                else
                {
                    Console.WriteLine("ModelState is invalid - returning to view with errors");
                    TempData["ErrorMessage"] = "Please fix the validation errors below.";
                }

                // If we get here, there were validation errors
                var currentLecturer = await _databaseService.GetUserAsync(userId);
                ViewBag.HourlyRate = currentLecturer?.HourlyRate ?? 0;
                ViewBag.MaxHours = 180;
                return View(claim);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in SubmitClaim: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred while submitting your claim: {ex.Message}";

                var currentLecturer = await _databaseService.GetUserAsync(userId);
                ViewBag.HourlyRate = currentLecturer?.HourlyRate ?? 0;
                ViewBag.MaxHours = 180;
                return View(claim);
            }
        }

        // Download supporting document
        [AuthorizeRole("Lecturer", "Coordinator", "Manager", "HR")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var fileResult = await _databaseService.DownloadDocumentAsync(
                id,
                _sessionService.GetUserId(),
                _sessionService.GetUserRole()
            );

            if (fileResult == null)
            {
                return NotFound();
            }

            return File(fileResult.FileContent, fileResult.ContentType ?? "application/octet-stream", fileResult.FileName);
        }

        // View Claims with Status Tracking
        [AuthorizeRole("Lecturer")]
        public async Task<IActionResult> MyClaims()
        {
            var claims = await _databaseService.GetUserClaimsAsync(_sessionService.GetUserId());
            return View(claims);
        }

        // Coordinator: View Pending Claims
        [AuthorizeRole("Coordinator")]
        public async Task<IActionResult> PendingClaims()
        {
            var claims = await _databaseService.GetPendingClaimsAsync();
            return View(claims);
        }

        // Coordinator: Approve Claim
        [AuthorizeRole("Coordinator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int id, string notes = "")
        {
            try
            {
                var success = await _databaseService.ApproveClaimAsync(
                    id,
                    _sessionService.GetUserName(),
                    notes
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Claim approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve claim. Please try again.";
                }
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
        public async Task<IActionResult> RejectClaim(int id, string notes)
        {
            try
            {
                if (string.IsNullOrEmpty(notes))
                {
                    TempData["ErrorMessage"] = "Rejection notes are required.";
                    return RedirectToAction("PendingClaims");
                }

                var success = await _databaseService.RejectClaimAsync(
                    id,
                    _sessionService.GetUserName(),
                    notes
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Claim rejected successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject claim. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
            }

            return RedirectToAction("PendingClaims");
        }

        // Manager: View Claims for Final Approval
        [AuthorizeRole("Manager")]
        public async Task<IActionResult> ManagerApproval()
        {
            var claims = await _databaseService.GetClaimsForManagerApprovalAsync();
            return View(claims);
        }

        // Manager: Final Approve Claim
        [AuthorizeRole("Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalApproveClaim(int id, string notes = "")
        {
            try
            {
                var success = await _databaseService.FinalApproveClaimAsync(
                    id,
                    _sessionService.GetUserName(),
                    notes
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Claim approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve claim. Please try again.";
                }
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
        public async Task<IActionResult> FinalRejectClaim(int id, string notes)
        {
            try
            {
                if (string.IsNullOrEmpty(notes))
                {
                    TempData["ErrorMessage"] = "Rejection notes are required.";
                    return RedirectToAction("ManagerApproval");
                }

                var success = await _databaseService.FinalRejectClaimAsync(
                    id,
                    _sessionService.GetUserName(),
                    notes
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Claim rejected successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject claim. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
            }

            return RedirectToAction("ManagerApproval");
        }

        // Claim Details with Status History
        [AuthorizeRole("Lecturer", "Coordinator", "Manager", "HR")]
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _databaseService.GetClaimDetailsAsync(
                id,
                _sessionService.GetUserId(),
                _sessionService.GetUserRole()
            );

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }
    }
}