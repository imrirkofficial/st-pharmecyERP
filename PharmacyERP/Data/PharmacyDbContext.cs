using Microsoft.EntityFrameworkCore;
using PharmacyERP.Models;

namespace PharmacyERP.Data
{
    public class PharmacyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\mssqllocaldb;Database=PharmacyERPDB;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    Password = "admin123", // In production, use hashed passwords
                    FullName = "System Administrator",
                    Role = "Admin",
                    Email = "admin@pharmacy.com",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Configure decimal precision
            modelBuilder.Entity<Medicine>()
                .Property(m => m.PurchasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Medicine>()
                .Property(m => m.SalePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleItem>()
                .Property(si => si.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Purchase>()
                .Property(p => p.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseItem>()
                .Property(pi => pi.UnitPrice)
                .HasPrecision(18, 2);
        }
    }
}
