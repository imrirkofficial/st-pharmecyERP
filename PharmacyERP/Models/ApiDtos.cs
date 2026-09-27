using System.Text.Json.Serialization;

namespace PharmacyERP.Models;

// Matches API JSON (camelCase via JsonSerializerDefaults.Web)
public record MedicineDto(int MedicineId, string MedicineName, string? GenericName, string? Manufacturer, string? Category, decimal PurchasePrice, decimal SalePrice, int StockQuantity, int ReorderLevel, string? Barcode, string? ShelfLocation, DateTime? ExpiryDate, bool IsActive, bool IsLowStock, bool IsExpiringSoon);
public record SupplierDto(int SupplierId, string SupplierName, string? Phone, decimal TotalPurchase, decimal OutstandingBalance, bool IsActive);
public record CustomerDto(int CustomerId, string CustomerName, string? Phone, decimal TotalPurchase, bool IsActive);
public record DashboardDto(decimal TodaySales, decimal MonthSales, int TodayInvoices, int TotalCustomers, int TotalSuppliers, List<LowStockItem> LowStock, List<RecentSale> Recent);
public record LowStockItem(int MedicineId, string MedicineName, int StockQuantity, int ReorderLevel);
public record RecentSale(int SaleId, string InvoiceNumber, DateTime SaleDate, decimal NetAmount, string PaymentMethod);
public record DailySalesDto(string Date, int InvoiceCount, decimal TotalAmount, decimal Discount, decimal NetRevenue, int ItemsSold);
public record ProfitDto(string From, string To, decimal Revenue, decimal Cogs, decimal Profit, int ItemsSold, int Invoices);
public record StockReportDto(int TotalMedicines, int LowStockCount, int OutOfStockCount, int Expiring30dCount, decimal StockPurchaseValue, decimal StockSaleValue);
