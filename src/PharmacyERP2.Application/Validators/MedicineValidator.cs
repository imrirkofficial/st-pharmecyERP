using FluentValidation;
using PharmacyERP2.Application.DTOs;

namespace PharmacyERP2.Application.Validators;

public class MedicineCreateValidator : AbstractValidator<MedicineCreateDto>
{
    public MedicineCreateValidator()
    {
        RuleFor(x => x.MedicineName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Barcode).MaximumLength(50);
    }
}
