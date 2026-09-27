using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PharmacyERP2.Core.Enums;

namespace PharmacyERP2.Core.Entities;

public class Sale : BaseEntity
{
    [Key]
    public int SaleId { get; set; }

    [Required, StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = global::PharmacyERP2.Core.Enums.PaymentMethod.Cash;

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}

public class SaleItem
{
    [Key]
    public int SaleItemId { get; set; }

    public int SaleId { get; set; }
    [ForeignKey(nameof(SaleId))]
    public Sale Sale { get; set; } = null!;

    public int MedicineId { get; set; }
    [ForeignKey(nameof(MedicineId))]
    public Medicine Medicine { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
