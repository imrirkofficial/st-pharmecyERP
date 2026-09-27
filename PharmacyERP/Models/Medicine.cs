using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Models
{
    public class Medicine
    {
        [Key]
        public int MedicineId { get; set; }

        [Required]
        [StringLength(200)]
        public string MedicineName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? GenericName { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(50)]
        public string? DosageForm { get; set; } // Tablet, Capsule, Syrup, Injection

        [StringLength(50)]
        public string? Strength { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; } // Piece, Box, Strip

        public decimal PurchasePrice { get; set; }

        public decimal SalePrice { get; set; }

        public int StockQuantity { get; set; }

        public int ReorderLevel { get; set; } = 10;

        [StringLength(50)]
        public string? Barcode { get; set; }

        [StringLength(50)]
        public string? ShelfLocation { get; set; }

        public DateTime? ManufactureDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
