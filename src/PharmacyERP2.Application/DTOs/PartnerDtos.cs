namespace PharmacyERP2.Application.DTOs;

public record SupplierCreateDto(string SupplierName, string? ContactPerson, string? Phone, string? Email, string? Address, string? City, string? Notes);
public record SupplierResponseDto(int SupplierId, string SupplierName, string? Phone, decimal TotalPurchase, decimal OutstandingBalance, bool IsActive);

public record CustomerCreateDto(string CustomerName, string? Phone, string? Email, string? Address, string? MedicalHistory, string? Allergies, string? Notes);
public record CustomerResponseDto(int CustomerId, string CustomerName, string? Phone, decimal TotalPurchase, bool IsActive);
