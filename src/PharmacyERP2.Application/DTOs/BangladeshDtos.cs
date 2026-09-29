namespace PharmacyERP2.Application.DTOs;

public record BranchDto(int BranchId, string BranchName, string? Address, string? Phone, bool IsHeadOffice, bool IsActive);
public record BranchCreateDto(string BranchName, string? Address, string? Phone, bool IsHeadOffice = false);

public record DoctorDto(int DoctorId, string DoctorName, string? Chamber, string? Phone, decimal CommissionPercent);
public record DoctorCreateDto(string DoctorName, string? Chamber, string? Phone, decimal CommissionPercent = 0);

public record DueCollectDto(int CustomerId, decimal Amount, string? Method, string? Notes);
public record SupplierPayDto(decimal Amount, string? Method, string? TrxId, int? PurchaseId, string? Notes);

public record ReturnItemDto(int MedicineId, int Quantity);
public record SaleReturnCreateDto(int SaleId, List<ReturnItemDto> Items, string? Reason);
public record PurchaseReturnCreateDto(int SupplierId, int? PurchaseId, string Reason, List<ReturnItemDto> Items);

public record ClosingCreateDto(DateTime BusinessDate, int? BranchId, string? Notes);
public record SmsCreateDto(string Phone, string Message, int? CustomerId);
public record RxLinkDto(int PrescriptionId, int DoctorId, string? ImagePath);
