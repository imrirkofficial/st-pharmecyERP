using Microsoft.EntityFrameworkCore;
using PharmacyERP2.Core.Entities;

namespace PharmacyERP2.Infrastructure.Data;

public class PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<DueTransaction> DueTransactions => Set<DueTransaction>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SmsLog> SmsLogs => Set<SmsLog>();
    public DbSet<DayClosing> DayClosings => Set<DayClosing>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        b.Entity<Medicine>().HasIndex(m => m.Barcode);
        b.Entity<Medicine>().HasIndex(m => m.MedicineName);
        b.Entity<Sale>().HasIndex(s => s.InvoiceNumber).IsUnique();
        b.Entity<Purchase>().HasIndex(p => p.PurchaseNumber).IsUnique();
        b.Entity<Prescription>().HasIndex(p => p.PrescriptionNumber).IsUnique();

        foreach (var e in new[] { "PurchasePrice", "SalePrice" })
            b.Entity<Medicine>().Property<decimal>(e).HasPrecision(18, 2);
        b.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
        b.Entity<Sale>().Property(s => s.NetAmount).HasPrecision(18, 2);
        b.Entity<Sale>().Property(s => s.PaidAmount).HasPrecision(18, 2);
        b.Entity<Sale>().Property(s => s.DiscountAmount).HasPrecision(18, 2);
        b.Entity<Sale>().Property(s => s.ChangeAmount).HasPrecision(18, 2);
        b.Entity<SaleItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Entity<SaleItem>().Property(i => i.TotalPrice).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(p => p.TotalAmount).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(p => p.PaidAmount).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(p => p.DueAmount).HasPrecision(18, 2);
        b.Entity<PurchaseItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Entity<PurchaseItem>().Property(i => i.TotalPrice).HasPrecision(18, 2);
        b.Entity<Customer>().Property(c => c.TotalPurchase).HasPrecision(18, 2);
        b.Entity<Supplier>().Property(s => s.TotalPurchase).HasPrecision(18, 2);
        b.Entity<Supplier>().Property(s => s.OutstandingBalance).HasPrecision(18, 2);

        b.Entity<Sale>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Purchase>().HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

        // ── Bangladesh module ──
        b.Entity<Sale>().HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SaleReturn>().HasIndex(r => r.ReturnNumber).IsUnique();
        b.Entity<PurchaseReturn>().HasIndex(r => r.ReturnNumber).IsUnique();
        b.Entity<DayClosing>().HasIndex(c => new { c.BusinessDate, c.BranchId }).IsUnique();
        foreach (var e in new[] { "DueAmount", "VatAmount" })
            b.Entity<Sale>().Property<decimal>(e).HasPrecision(18, 2);
        b.Entity<Customer>().Property(c => c.DueBalance).HasPrecision(18, 2);
        b.Entity<Medicine>().Property(m => m.VatPercent).HasPrecision(5, 2);
        b.Entity<Medicine>().Property(m => m.UnitsPerStrip).HasDefaultValue(10);
        b.Entity<Medicine>().Property(m => m.StripsPerBox).HasDefaultValue(10);
        b.Entity<SalePayment>().Property(p => p.Amount).HasPrecision(18, 2);
        b.Entity<DueTransaction>().Property(t => t.Amount).HasPrecision(18, 2);
        b.Entity<SupplierPayment>().Property(p => p.Amount).HasPrecision(18, 2);
        b.Entity<SaleReturn>().Property(r => r.RefundAmount).HasPrecision(18, 2);
        b.Entity<SaleReturnItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Entity<PurchaseReturn>().Property(r => r.ReturnAmount).HasPrecision(18, 2);
        b.Entity<PurchaseReturnItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Entity<Doctor>().Property(d => d.CommissionPercent).HasPrecision(5, 2);
        foreach (var e in new[] { "CashSales", "MobileSales", "CardSales", "DueSales", "DueCollected", "PurchasePaid", "TotalSales", "TotalProfit" })
            b.Entity<DayClosing>().Property<decimal>(e).HasPrecision(18, 2);
    }
}
