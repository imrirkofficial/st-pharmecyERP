using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyERP2.Core.Entities;

// ── Branch (Single shop = 1 row "Main"; Multi mode = many) ──────────────
public class Branch : BaseEntity
{
    [Key]
    public int BranchId { get; set; }

    [Required, StringLength(150)]
    public string BranchName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public bool IsHeadOffice { get; set; }
}

// ── Doctor (prescription + commission tracking) ─────────────────────────
public class Doctor : BaseEntity
{
    [Key]
    public int DoctorId { get; set; }

    [Required, StringLength(150)]
    public string DoctorName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Chamber { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>Referral commission percent (0 if none).</summary>
    public decimal CommissionPercent { get; set; }
}

// ── Split payment per sale: Cash / bKash / Nagad / Rocket / Card ────────
public class SalePayment
{
    [Key]
    public int SalePaymentId { get; set; }

    public int SaleId { get; set; }
    [ForeignKey(nameof(SaleId))]
    public Sale Sale { get; set; } = null!;

    [StringLength(30)]
    public string Method { get; set; } = "Cash";

    public decimal Amount { get; set; }

    [StringLength(100)]
    public string? TrxId { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}

// ── Customer due ledger (Baki Khata): Due (+) / Collect (−) ─────────────
public class DueTransaction
{
    [Key]
    public int DueTransactionId { get; set; }

    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    public int? SaleId { get; set; }

    /// <summary>Positive = due added, negative = collected.</summary>
    public decimal Amount { get; set; }

    [StringLength(20)]
    public string Type { get; set; } = "Due"; // Due | Collect

    [StringLength(30)]
    public string? Method { get; set; }

    public int UserId { get; set; }
    public DateTime TxnDate { get; set; } = DateTime.UtcNow;

    [StringLength(300)]
    public string? Notes { get; set; }
}

// ── Supplier payment ledger (company paona) ─────────────────────────────
public class SupplierPayment
{
    [Key]
    public int SupplierPaymentId { get; set; }

    public int SupplierId { get; set; }
    [ForeignKey(nameof(SupplierId))]
    public Supplier Supplier { get; set; } = null!;

    public int? PurchaseId { get; set; }

    public decimal Amount { get; set; }

    [StringLength(30)]
    public string? Method { get; set; }

    [StringLength(100)]
    public string? TrxId { get; set; }

    public int UserId { get; set; }
    public DateTime PayDate { get; set; } = DateTime.UtcNow;

    [StringLength(300)]
    public string? Notes { get; set; }
}

// ── Sales return (customer ferot) ───────────────────────────────────────
public class SaleReturn : BaseEntity
{
    [Key]
    public int SaleReturnId { get; set; }

    [Required, StringLength(50)]
    public string ReturnNumber { get; set; } = string.Empty;

    public int SaleId { get; set; }
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public decimal RefundAmount { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public ICollection<SaleReturnItem> Items { get; set; } = new List<SaleReturnItem>();
}

public class SaleReturnItem
{
    [Key]
    public int SaleReturnItemId { get; set; }

    public int SaleReturnId { get; set; }
    [ForeignKey(nameof(SaleReturnId))]
    public SaleReturn SaleReturn { get; set; } = null!;

    public int MedicineId { get; set; }
    [ForeignKey(nameof(MedicineId))]
    public Medicine Medicine { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// ── Purchase return (company ke expiry/damage ferot) ────────────────────
public class PurchaseReturn : BaseEntity
{
    [Key]
    public int PurchaseReturnId { get; set; }

    [Required, StringLength(50)]
    public string ReturnNumber { get; set; } = string.Empty;

    public int? PurchaseId { get; set; }

    public int SupplierId { get; set; }
    [ForeignKey(nameof(SupplierId))]
    public Supplier Supplier { get; set; } = null!;

    public int UserId { get; set; }
    public int? BranchId { get; set; }

    [StringLength(20)]
    public string Reason { get; set; } = "Expired"; // Expired | Damaged | Other

    public decimal ReturnAmount { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}

public class PurchaseReturnItem
{
    [Key]
    public int PurchaseReturnItemId { get; set; }

    public int PurchaseReturnId { get; set; }
    [ForeignKey(nameof(PurchaseReturnId))]
    public PurchaseReturn PurchaseReturn { get; set; } = null!;

    public int MedicineId { get; set; }
    [ForeignKey(nameof(MedicineId))]
    public Medicine Medicine { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// ── Audit log (ke ki korlo) ─────────────────────────────────────────────
public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    public int UserId { get; set; }

    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Entity { get; set; }

    public int? EntityId { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }

    public DateTime LogDate { get; set; } = DateTime.UtcNow;
}

// ── SMS outbox (SSL Wireless / BulkSMS BD pore wire hobe) ───────────────
public class SmsLog : BaseEntity
{
    [Key]
    public int SmsLogId { get; set; }

    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(20)]
    public string Status { get; set; } = "Queued"; // Queued | Sent | Failed

    public int? CustomerId { get; set; }
}

// ── Daily closing snapshot (din sesh hisab) ─────────────────────────────
public class DayClosing : BaseEntity
{
    [Key]
    public int DayClosingId { get; set; }

    public DateTime BusinessDate { get; set; }
    public int? BranchId { get; set; }
    public int UserId { get; set; }

    public decimal CashSales { get; set; }
    public decimal MobileSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal DueSales { get; set; }
    public decimal DueCollected { get; set; }
    public decimal PurchasePaid { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalProfit { get; set; }
    public int InvoiceCount { get; set; }

    public DateTime ClosedAt { get; set; } = DateTime.UtcNow;

    [StringLength(300)]
    public string? Notes { get; set; }
}
