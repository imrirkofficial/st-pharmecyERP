namespace PharmacyERP2.Application.DTOs;

public record PrescriptionItemCreateDto(int MedicineId, string Dosage, int Quantity, int Duration, string? Instructions);
public record PrescriptionCreateDto(int CustomerId, string? DoctorName, string? Diagnosis, string? Notes, List<PrescriptionItemCreateDto> Items);
public record PrescriptionItemResponseDto(int MedicineId, string MedicineName, string Dosage, int Quantity, int Duration, string? Instructions);
public record PrescriptionResponseDto(int PrescriptionId, string PrescriptionNumber, DateTime PrescriptionDate, int CustomerId, string? CustomerName, string? DoctorName, string? Diagnosis, bool IsFulfilled, DateTime? FulfilledDate, List<PrescriptionItemResponseDto> Items);
public record PrescriptionFulfillDto(decimal DiscountAmount, decimal PaidAmount, string PaymentMethod);
