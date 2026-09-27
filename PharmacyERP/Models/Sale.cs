using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyERP.Models
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime SaleDate { get; set; } = DateTime.Now;

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal NetAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Mobile Banking

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }

    public class SaleItem
    {
        [Key]
        public int SaleItemId { get; set; }

        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public Sale Sale { get; set; } = null!;

        public int MedicineId { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine Medicine { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
