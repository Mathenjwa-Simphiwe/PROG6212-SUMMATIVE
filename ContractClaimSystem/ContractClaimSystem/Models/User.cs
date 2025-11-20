namespace ContractClaimSystem.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "Lecturer", "Coordinator", "Manager", "HR"
        public decimal HourlyRate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
