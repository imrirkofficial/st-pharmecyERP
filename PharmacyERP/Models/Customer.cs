using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? MedicalHistory { get; set; }

        [StringLength(500)]
        public string? Allergies { get; set; }

        public decimal TotalPurchase { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
