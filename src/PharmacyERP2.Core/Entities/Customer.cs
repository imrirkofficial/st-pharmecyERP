using System.ComponentModel.DataAnnotations;

namespace PharmacyERP2.Core.Entities;

public class Customer : BaseEntity
{
    [Key]
    public int CustomerId { get; set; }

    [Required, StringLength(100)]
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

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
