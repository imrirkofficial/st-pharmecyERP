using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyERP.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseId { get; set; }

        [Required]
        [StringLength(50)]
        public string PurchaseNumber { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; } = null!;

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DueAmount { get; set; }

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Paid, Partial, Pending

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    }

    public class PurchaseItem
    {
        [Key]
        public int PurchaseItemId { get; set; }

        public int PurchaseId { get; set; }

        [ForeignKey("PurchaseId")]
        public Purchase Purchase { get; set; } = null!;

        public int MedicineId { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine Medicine { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }
    }
}
