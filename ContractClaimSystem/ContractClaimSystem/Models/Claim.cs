namespace ContractClaimSystem.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        public int LecturerId { get; set; }
        public User Lecturer { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        public string FileName { get; set; }
        public byte[] FileContent { get; set; }
        public string Status { get; set; } // "Pending", "Approved", "Rejected"
        public DateTime SubmittedDate { get; set; }
        public DateTime? CoordinatorApprovedDate { get; set; }
        public DateTime? ManagerApprovedDate { get; set; }
    }
}
