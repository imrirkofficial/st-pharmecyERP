using System.ComponentModel.DataAnnotations;

namespace PharmacyERP2.Core.Entities;

public class Supplier : BaseEntity
{
    [Key]
    public int SupplierId { get; set; }

    [Required, StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? Country { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    public decimal TotalPurchase { get; set; }
    public decimal OutstandingBalance { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
