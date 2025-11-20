using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractClaimSystem.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required]
        public int LecturerId { get; set; }

        [ForeignKey("LecturerId")]
        public User Lecturer { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }

        public string FileName { get; set; }

        public byte[] FileContent { get; set; }

        public string ContentType { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedDate { get; set; }

        public DateTime? CoordinatorApprovedDate { get; set; }

        public DateTime? ManagerApprovedDate { get; set; }

        public List<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();
    }
}