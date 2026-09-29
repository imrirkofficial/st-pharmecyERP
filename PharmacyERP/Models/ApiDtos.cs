using System.Text.Json.Serialization;

namespace PharmacyERP.Models;

// Matches API JSON (camelCase via JsonSerializerDefaults.Web)
public record MedicineDto(int MedicineId, string MedicineName, string? GenericName, string? Manufacturer, string? Category, decimal PurchasePrice, decimal SalePrice, int StockQuantity, int ReorderLevel, string? Barcode, string? ShelfLocation, DateTime? ExpiryDate, bool IsActive, bool IsLowStock, bool IsExpiringSoon, int UnitsPerStrip = 10, int StripsPerBox = 10, bool IsControlled = false, decimal VatPercent = 0);
public record SaleCheckoutDto(int SaleId, string InvoiceNumber, DateTime SaleDate, int? CustomerId, string? CustomerName, decimal TotalAmount, decimal DiscountAmount, decimal NetAmount, decimal PaidAmount, decimal ChangeAmount, string PaymentMethod, object? Items, decimal DueAmount = 0, decimal VatAmount = 0);
public record SupplierDto(int SupplierId, string SupplierName, string? Phone, decimal TotalPurchase, decimal OutstandingBalance, bool IsActive);
public record CustomerDto(int CustomerId, string CustomerName, string? Phone, decimal TotalPurchase, bool IsActive);
public record DashboardDto(decimal TodaySales, decimal MonthSales, int TodayInvoices, int TotalCustomers, int TotalSuppliers, List<LowStockItem> LowStock, List<RecentSale> Recent);
public record LowStockItem(int MedicineId, string MedicineName, int StockQuantity, int ReorderLevel);
public record RecentSale(int SaleId, string InvoiceNumber, DateTime SaleDate, decimal NetAmount, string PaymentMethod);
public record DailySalesDto(string Date, int InvoiceCount, decimal TotalAmount, decimal Discount, decimal NetRevenue, int ItemsSold);
public record ProfitDto(string From, string To, decimal Revenue, decimal Cogs, decimal Profit, int ItemsSold, int Invoices);
public record StockReportDto(int TotalMedicines, int LowStockCount, int OutOfStockCount, int Expiring30dCount, decimal StockPurchaseValue, decimal StockSaleValue);
public record BranchDto(int BranchId, string BranchName, string? Address, string? Phone, bool IsHeadOffice, bool IsActive);
public record DoctorDto(int DoctorId, string DoctorName, string? Chamber, string? Phone, decimal CommissionPercent);
public record DueCustomerDto(int CustomerId, string CustomerName, string? Phone, decimal DueBalance, decimal TotalPurchase);
public record DuesDto(decimal TotalDue, List<DueCustomerDto> Items);
public record LedgerDto(int DueTransactionId, int? SaleId, decimal Amount, string Type, string? Method, DateTime TxnDate, string? Notes);
public record ExpiryReturnDto(int MedicineId, string MedicineName, string? Manufacturer, DateTime? ExpiryDate, int StockQuantity, decimal PurchasePrice, decimal StockValue);
public record ExpiryListDto(int Days, int Count, List<ExpiryReturnDto> Items);
public record ClosingDto(int DayClosingId, DateTime BusinessDate, int? BranchId, int InvoiceCount, decimal TotalSales, decimal CashSales, decimal MobileSales, decimal CardSales, decimal DueSales, decimal DueCollected, decimal PurchasePaid, decimal TotalProfit, DateTime ClosedAt, string? Notes);
public record VatDto(string From, string To, int Invoices, decimal Revenue, decimal VatCollected, decimal DueTotal);
public record ReorderDto(int MedicineId, string MedicineName, string? Manufacturer, int StockQuantity, int ReorderLevel, int SuggestedQty, decimal PurchasePrice);
public record ExpiringItem(int MedicineId, string MedicineName, DateTime? ExpiryDate, int StockQuantity);
public record TopItem(int MedicineId, string MedicineName, int Qty, decimal Revenue);
public record DashboardExtDto(List<ExpiringItem> Expiring, List<TopItem> TopSelling);
