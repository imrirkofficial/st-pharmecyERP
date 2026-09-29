namespace PharmacyERP2.Application.DTOs;

public record SalePaymentDto(string Method, decimal Amount, string? TrxId = null);
public record SaleItemDto(int MedicineId, int Quantity, decimal? UnitPrice = null);
public record SaleCreateDto(int? CustomerId, List<SaleItemDto> Items, decimal DiscountAmount, decimal PaidAmount, string PaymentMethod, string? Notes,
    string? TrxId = null, int? BranchId = null, decimal VatAmount = 0, List<SalePaymentDto>? Payments = null);
public record SaleItemResponseDto(int MedicineId, string MedicineName, int Quantity, decimal UnitPrice, decimal TotalPrice);
public record SaleResponseDto(int SaleId, string InvoiceNumber, DateTime SaleDate, int? CustomerId, string? CustomerName, decimal TotalAmount, decimal DiscountAmount, decimal NetAmount, decimal PaidAmount, decimal ChangeAmount, string PaymentMethod, List<SaleItemResponseDto> Items, decimal DueAmount = 0, decimal VatAmount = 0);

public record PurchaseItemDto(int MedicineId, int Quantity, decimal UnitPrice, DateTime? ExpiryDate = null, string? BatchNumber = null);
public record PurchaseCreateDto(int SupplierId, List<PurchaseItemDto> Items, decimal PaidAmount, string? Notes);
public record PurchaseItemResponseDto(int MedicineId, string MedicineName, int Quantity, decimal UnitPrice, decimal TotalPrice, string? BatchNumber);
public record PurchaseResponseDto(int PurchaseId, string PurchaseNumber, DateTime PurchaseDate, int SupplierId, string? SupplierName, decimal TotalAmount, decimal PaidAmount, decimal DueAmount, string PaymentStatus, List<PurchaseItemResponseDto> Items);
