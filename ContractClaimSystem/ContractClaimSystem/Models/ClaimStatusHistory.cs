using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractClaimSystem.Models
{
    public class ClaimStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [ForeignKey("ClaimId")]
        public Claim Claim { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string ActionBy { get; set; }

        public string Notes { get; set; }

        [Required]
        public DateTime ActionDate { get; set; }
    }
}
