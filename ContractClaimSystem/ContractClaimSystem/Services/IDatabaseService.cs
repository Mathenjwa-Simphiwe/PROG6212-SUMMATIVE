using ContractClaimSystem.Data;
using ContractClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractClaimSystem.Services
{
    public interface IDatabaseService
    {
        Task<bool> SubmitClaimAsync(Claim claim, IFormFile supportingDocument, int lecturerId, string lecturerName);
        Task<List<Claim>> GetUserClaimsAsync(int userId);
        Task<List<User>> GetUsersAsync();
        Task<User> GetUserAsync(int userId);
        Task<bool> AddUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<List<Claim>> GetPendingClaimsAsync();
        Task<List<Claim>> GetClaimsForManagerApprovalAsync();
        Task<bool> ApproveClaimAsync(int claimId, string approverName, string notes = "");
        Task<bool> RejectClaimAsync(int claimId, string rejectorName, string notes);
        Task<bool> FinalApproveClaimAsync(int claimId, string approverName, string notes = "");
        Task<bool> FinalRejectClaimAsync(int claimId, string rejectorName, string notes);
        Task<Claim> GetClaimDetailsAsync(int claimId, int currentUserId, string currentUserRole);
        Task<FileDownloadResult> DownloadDocumentAsync(int claimId, int currentUserId, string currentUserRole);
        Task<List<Claim>> GetClaimsReportAsync(int? month, int? year, string status);
        Task<User> GetUserByEmailAsync(string email);
    }

    public class FileDownloadResult
    {
        public byte[] FileContent { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly ApplicationDbContext _sqlContext;
        private readonly ApplicationDbContextInMemory _memoryContext;
        private readonly ILogger<DatabaseService> _logger;
        private bool _useSqlServer = false;

        public DatabaseService(ApplicationDbContext sqlContext, ApplicationDbContextInMemory memoryContext, ILogger<DatabaseService> logger)
        {
            _sqlContext = sqlContext;
            _memoryContext = memoryContext;
            _logger = logger;

            // Check if SQL Server is available
            try
            {
                // Test SQL Server connection
                _useSqlServer = _sqlContext.Database.CanConnect();
                Console.WriteLine($"SQL Server available: {_useSqlServer}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Server not available: {ex.Message}");
                _useSqlServer = false;
            }

            Console.WriteLine($"Using database: {(_useSqlServer ? "SQL Server" : "In-Memory")}");
        }

        public async Task<bool> SubmitClaimAsync(Claim claim, IFormFile supportingDocument, int lecturerId, string lecturerName)
        {
            try
            {
                if (_useSqlServer)
                {
                    return await SubmitClaimToSqlAsync(claim, supportingDocument, lecturerId, lecturerName);
                }
                else
                {
                    return await SubmitClaimToMemoryAsync(claim, supportingDocument, lecturerId, lecturerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim");
                return false;
            }
        }

        private async Task<bool> SubmitClaimToSqlAsync(Claim claim, IFormFile supportingDocument, int lecturerId, string lecturerName)
        {
            try
            {
                var lecturer = await _sqlContext.Users.FindAsync(lecturerId);
                if (lecturer == null)
                {
                    Console.WriteLine($"Lecturer with ID {lecturerId} not found in SQL database");
                    return false;
                }

                var newClaim = new Claim
                {
                    LecturerId = lecturerId,
                    Month = claim.Month,
                    Year = claim.Year,
                    HoursWorked = claim.HoursWorked,
                    HourlyRate = lecturer.HourlyRate,
                    TotalAmount = claim.HoursWorked * lecturer.HourlyRate,
                    Notes = claim.Notes ?? "",
                    Status = "Pending",
                    SubmittedDate = DateTime.Now,
                    FileName = "",
                    FileContent = new byte[0],
                    ContentType = ""
                };

                if (supportingDocument != null && supportingDocument.Length > 0)
                {
                    Console.WriteLine($"Processing file upload: {supportingDocument.FileName}");
                    await HandleFileUpload(newClaim, supportingDocument);
                }

                _sqlContext.Claims.Add(newClaim);

                // Add status history
                newClaim.StatusHistory.Add(new ClaimStatusHistory
                {
                    Status = "Pending",
                    ActionBy = lecturerName,
                    Notes = "Claim submitted",
                    ActionDate = DateTime.Now
                });

                var result = await _sqlContext.SaveChangesAsync();
                Console.WriteLine($"SQL SaveChanges result: {result} rows affected");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitClaimToSqlAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> SubmitClaimToMemoryAsync(Claim claim, IFormFile supportingDocument, int lecturerId, string lecturerName)
        {
            try
            {
                var lecturer = await _memoryContext.Users.FindAsync(lecturerId);
                if (lecturer == null)
                {
                    Console.WriteLine($"Lecturer with ID {lecturerId} not found in In-Memory database");
                    return false;
                }

                var newClaim = new Claim
                {
                    LecturerId = lecturerId,
                    Month = claim.Month,
                    Year = claim.Year,
                    HoursWorked = claim.HoursWorked,
                    HourlyRate = lecturer.HourlyRate,
                    TotalAmount = claim.HoursWorked * lecturer.HourlyRate,
                    Notes = claim.Notes ?? "",
                    Status = "Pending",
                    SubmittedDate = DateTime.Now,
                    FileName = "",
                    FileContent = new byte[0],
                    ContentType = ""
                };

                if (supportingDocument != null && supportingDocument.Length > 0)
                {
                    Console.WriteLine($"Processing file upload: {supportingDocument.FileName}");
                    await HandleFileUpload(newClaim, supportingDocument);
                }

                _memoryContext.Claims.Add(newClaim);

                // Add status history
                newClaim.StatusHistory.Add(new ClaimStatusHistory
                {
                    Status = "Pending",
                    ActionBy = lecturerName,
                    Notes = "Claim submitted",
                    ActionDate = DateTime.Now
                });

                var result = await _memoryContext.SaveChangesAsync();
                Console.WriteLine($"In-Memory SaveChanges result: {result} rows affected");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitClaimToMemoryAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<Claim>> GetUserClaimsAsync(int userId)
        {
            if (_useSqlServer)
            {
                try
                {
                    return await _sqlContext.Claims
                        .Include(c => c.StatusHistory)
                        .Where(c => c.LecturerId == userId)
                        .OrderByDescending(c => c.SubmittedDate)
                        .ToListAsync();
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            return await _memoryContext.Claims
                .Include(c => c.StatusHistory)
                .Where(c => c.LecturerId == userId)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Claim>> GetPendingClaimsAsync()
        {
            if (_useSqlServer)
            {
                try
                {
                    return await _sqlContext.Claims
                        .Include(c => c.Lecturer)
                        .Include(c => c.StatusHistory)
                        .Where(c => c.Status == "Pending")
                        .OrderBy(c => c.SubmittedDate)
                        .ToListAsync();
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            return await _memoryContext.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.StatusHistory)
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<Claim>> GetClaimsForManagerApprovalAsync()
        {
            if (_useSqlServer)
            {
                try
                {
                    return await _sqlContext.Claims
                        .Include(c => c.Lecturer)
                        .Include(c => c.StatusHistory)
                        .Where(c => c.Status == "ApprovedByCoordinator")
                        .OrderBy(c => c.CoordinatorApprovedDate)
                        .ToListAsync();
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            return await _memoryContext.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.StatusHistory)
                .Where(c => c.Status == "ApprovedByCoordinator")
                .OrderBy(c => c.CoordinatorApprovedDate)
                .ToListAsync();
        }

        public async Task<bool> ApproveClaimAsync(int claimId, string approverName, string notes = "")
        {
            try
            {
                if (_useSqlServer)
                {
                    return await ApproveClaimInSqlAsync(claimId, approverName, notes);
                }
                else
                {
                    return await ApproveClaimInMemoryAsync(claimId, approverName, notes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim");
                return false;
            }
        }

        private async Task<bool> ApproveClaimInSqlAsync(int claimId, string approverName, string notes)
        {
            var claim = await _sqlContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "ApprovedByCoordinator";
            claim.CoordinatorApprovedDate = DateTime.Now;

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "ApprovedByCoordinator",
                ActionBy = approverName,
                Notes = notes ?? "Approved by Coordinator",
                ActionDate = DateTime.Now
            });

            await _sqlContext.SaveChangesAsync();
            return true;
        }

        private async Task<bool> ApproveClaimInMemoryAsync(int claimId, string approverName, string notes)
        {
            var claim = await _memoryContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "ApprovedByCoordinator";
            claim.CoordinatorApprovedDate = DateTime.Now;

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "ApprovedByCoordinator",
                ActionBy = approverName,
                Notes = notes ?? "Approved by Coordinator",
                ActionDate = DateTime.Now
            });

            await _memoryContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectClaimAsync(int claimId, string rejectorName, string notes)
        {
            try
            {
                if (_useSqlServer)
                {
                    return await RejectClaimInSqlAsync(claimId, rejectorName, notes);
                }
                else
                {
                    return await RejectClaimInMemoryAsync(claimId, rejectorName, notes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim");
                return false;
            }
        }

        private async Task<bool> RejectClaimInSqlAsync(int claimId, string rejectorName, string notes)
        {
            var claim = await _sqlContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "Rejected";

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "Rejected",
                ActionBy = rejectorName,
                Notes = notes,
                ActionDate = DateTime.Now
            });

            await _sqlContext.SaveChangesAsync();
            return true;
        }

        private async Task<bool> RejectClaimInMemoryAsync(int claimId, string rejectorName, string notes)
        {
            var claim = await _memoryContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "Rejected";

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "Rejected",
                ActionBy = rejectorName,
                Notes = notes,
                ActionDate = DateTime.Now
            });

            await _memoryContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FinalApproveClaimAsync(int claimId, string approverName, string notes = "")
        {
            try
            {
                if (_useSqlServer)
                {
                    return await FinalApproveClaimInSqlAsync(claimId, approverName, notes);
                }
                else
                {
                    return await FinalApproveClaimInMemoryAsync(claimId, approverName, notes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error final approving claim");
                return false;
            }
        }

        private async Task<bool> FinalApproveClaimInSqlAsync(int claimId, string approverName, string notes)
        {
            var claim = await _sqlContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "Approved";
            claim.ManagerApprovedDate = DateTime.Now;

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "Approved",
                ActionBy = approverName,
                Notes = notes ?? "Approved by Academic Manager",
                ActionDate = DateTime.Now
            });

            await _sqlContext.SaveChangesAsync();
            return true;
        }

        private async Task<bool> FinalApproveClaimInMemoryAsync(int claimId, string approverName, string notes)
        {
            var claim = await _memoryContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "Approved";
            claim.ManagerApprovedDate = DateTime.Now;

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "Approved",
                ActionBy = approverName,
                Notes = notes ?? "Approved by Academic Manager",
                ActionDate = DateTime.Now
            });

            await _memoryContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FinalRejectClaimAsync(int claimId, string rejectorName, string notes)
        {
            try
            {
                if (_useSqlServer)
                {
                    return await FinalRejectClaimInSqlAsync(claimId, rejectorName, notes);
                }
                else
                {
                    return await FinalRejectClaimInMemoryAsync(claimId, rejectorName, notes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error final rejecting claim");
                return false;
            }
        }

        private async Task<bool> FinalRejectClaimInSqlAsync(int claimId, string rejectorName, string notes)
        {
            var claim = await _sqlContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "Rejected";

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "Rejected",
                ActionBy = rejectorName,
                Notes = notes,
                ActionDate = DateTime.Now
            });

            await _sqlContext.SaveChangesAsync();
            return true;
        }

        private async Task<bool> FinalRejectClaimInMemoryAsync(int claimId, string rejectorName, string notes)
        {
            var claim = await _memoryContext.Claims
                .Include(c => c.StatusHistory)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            claim.Status = "Rejected";

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                Status = "Rejected",
                ActionBy = rejectorName,
                Notes = notes,
                ActionDate = DateTime.Now
            });

            await _memoryContext.SaveChangesAsync();
            return true;
        }

        public async Task<Claim> GetClaimDetailsAsync(int claimId, int currentUserId, string currentUserRole)
        {
            Claim claim = null;

            if (_useSqlServer)
            {
                try
                {
                    claim = await _sqlContext.Claims
                        .Include(c => c.Lecturer)
                        .Include(c => c.StatusHistory)
                        .FirstOrDefaultAsync(c => c.ClaimId == claimId);
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            if (claim == null)
            {
                claim = await _memoryContext.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.StatusHistory)
                    .FirstOrDefaultAsync(c => c.ClaimId == claimId);
            }

            // Authorization check
            if (claim != null && currentUserRole == "Lecturer" && claim.LecturerId != currentUserId)
            {
                return null;
            }

            return claim;
        }

        public async Task<FileDownloadResult> DownloadDocumentAsync(int claimId, int currentUserId, string currentUserRole)
        {
            Claim claim = null;

            if (_useSqlServer)
            {
                try
                {
                    claim = await _sqlContext.Claims.FindAsync(claimId);
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            if (claim == null)
            {
                claim = await _memoryContext.Claims.FindAsync(claimId);
            }

            if (claim?.FileContent == null) return null;

            // Authorization check
            if (currentUserRole == "Lecturer" && claim.LecturerId != currentUserId)
            {
                return null;
            }

            return new FileDownloadResult
            {
                FileContent = claim.FileContent,
                ContentType = claim.ContentType,
                FileName = claim.FileName
            };
        }

        public async Task<List<Claim>> GetClaimsReportAsync(int? month, int? year, string status)
        {
            try
            {
                IQueryable<Claim> claimsQuery;

                if (_useSqlServer)
                {
                    try
                    {
                        claimsQuery = _sqlContext.Claims
                            .Include(c => c.Lecturer)
                            .AsQueryable();
                    }
                    catch
                    {
                        _useSqlServer = false;
                        claimsQuery = _memoryContext.Claims
                            .Include(c => c.Lecturer)
                            .AsQueryable();
                    }
                }
                else
                {
                    claimsQuery = _memoryContext.Claims
                        .Include(c => c.Lecturer)
                        .AsQueryable();
                }

                // Apply filters
                if (month.HasValue && month > 0)
                    claimsQuery = claimsQuery.Where(c => c.Month == month.Value);

                if (year.HasValue && year > 0)
                    claimsQuery = claimsQuery.Where(c => c.Year == year.Value);

                if (!string.IsNullOrEmpty(status))
                    claimsQuery = claimsQuery.Where(c => c.Status == status);

                return await claimsQuery
                    .OrderByDescending(c => c.SubmittedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating claims report");
                return new List<Claim>();
            }
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (_useSqlServer)
            {
                try
                {
                    return await _sqlContext.Users
                        .FirstOrDefaultAsync(u => u.Email == email);
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            return await _memoryContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserAsync(int userId)
        {
            if (_useSqlServer)
            {
                try
                {
                    return await _sqlContext.Users.FindAsync(userId);
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            return await _memoryContext.Users.FindAsync(userId);
        }

        public async Task<List<User>> GetUsersAsync()
        {
            if (_useSqlServer)
            {
                try
                {
                    return await _sqlContext.Users.ToListAsync();
                }
                catch
                {
                    _useSqlServer = false;
                }
            }

            return await _memoryContext.Users.ToListAsync();
        }

        public async Task<bool> AddUserAsync(User user)
        {
            try
            {
                // Check if email already exists
                var existingUser = await GetUserByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    return false; // Email already exists
                }

                // Set created date
                user.CreatedDate = DateTime.Now;

                // If not a lecturer, set hourly rate to 0
                if (user.Role != "Lecturer")
                {
                    user.HourlyRate = 0;
                }

                if (_useSqlServer)
                {
                    try
                    {
                        _sqlContext.Users.Add(user);
                        await _sqlContext.SaveChangesAsync();
                        return true;
                    }
                    catch
                    {
                        _useSqlServer = false;
                    }
                }

                _memoryContext.Users.Add(user);
                await _memoryContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                User existingUser;

                if (_useSqlServer)
                {
                    try
                    {
                        existingUser = await _sqlContext.Users.FindAsync(user.UserId);
                        if (existingUser != null)
                        {
                            await UpdateUserProperties(existingUser, user);
                            await _sqlContext.SaveChangesAsync();
                            return true;
                        }
                    }
                    catch
                    {
                        _useSqlServer = false;
                    }
                }

                existingUser = await _memoryContext.Users.FindAsync(user.UserId);
                if (existingUser == null) return false;

                await UpdateUserProperties(existingUser, user);
                await _memoryContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return false;
            }
        }

        private async Task UpdateUserProperties(User existingUser, User newUser)
        {
            // Check if email is being changed and if it already exists
            if (existingUser.Email != newUser.Email)
            {
                var emailExists = await GetUserByEmailAsync(newUser.Email);
                if (emailExists != null && emailExists.UserId != newUser.UserId)
                {
                    throw new Exception("A user with this email already exists.");
                }
            }

            // Update properties
            existingUser.FirstName = newUser.FirstName;
            existingUser.LastName = newUser.LastName;
            existingUser.Email = newUser.Email;
            existingUser.Role = newUser.Role;
            existingUser.HourlyRate = newUser.Role == "Lecturer" ? newUser.HourlyRate : 0;

            // Only update password if a new one was provided
            if (!string.IsNullOrEmpty(newUser.Password))
            {
                existingUser.Password = newUser.Password;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                User user;

                if (_useSqlServer)
                {
                    try
                    {
                        user = await _sqlContext.Users.FindAsync(userId);
                        if (user != null)
                        {
                            // Prevent deletion of the last HR user
                            if (user.Role == "HR")
                            {
                                var hrCount = await _sqlContext.Users.CountAsync(u => u.Role == "HR");
                                if (hrCount <= 1)
                                {
                                    throw new Exception("Cannot delete the last HR user.");
                                }
                            }

                            _sqlContext.Users.Remove(user);
                            await _sqlContext.SaveChangesAsync();
                            return true;
                        }
                    }
                    catch (Exception ex) when (ex.Message != "Cannot delete the last HR user.")
                    {
                        _useSqlServer = false;
                    }
                }

                user = await _memoryContext.Users.FindAsync(userId);
                if (user == null) return false;

                // Prevent deletion of the last HR user
                if (user.Role == "HR")
                {
                    var hrCount = await _memoryContext.Users.CountAsync(u => u.Role == "HR");
                    if (hrCount <= 1)
                    {
                        throw new Exception("Cannot delete the last HR user.");
                    }
                }

                _memoryContext.Users.Remove(user);
                await _memoryContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                throw; // Re-throw to be handled by controller
            }
        }

      
        private async Task HandleFileUpload(Claim claim, IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024)
                throw new Exception("File size must be less than 5MB.");

            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                throw new Exception("Only PDF, DOCX, XLSX, JPG, and PNG files are allowed.");

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                claim.FileName = file.FileName;
                claim.FileContent = memoryStream.ToArray();
                claim.ContentType = file.ContentType;
            }
        }
    }
}