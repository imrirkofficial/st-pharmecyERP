using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyERP2.Application.DTOs;
using PharmacyERP2.Core.Entities;
using PharmacyERP2.Infrastructure.Data;

namespace PharmacyERP2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController(PharmacyDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int? customerId, [FromQuery] bool? fulfilled, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Prescriptions.Include(p => p.Customer).AsNoTracking().OrderByDescending(p => p.PrescriptionDate).AsQueryable();
        if (customerId.HasValue) q = q.Where(p => p.CustomerId == customerId.Value);
        if (fulfilled.HasValue) q = q.Where(p => p.IsFulfilled == fulfilled.Value);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { p.PrescriptionId, p.PrescriptionNumber, p.PrescriptionDate, p.CustomerId, CustomerName = p.Customer.CustomerName, p.DoctorName, p.Diagnosis, p.IsFulfilled })
            .ToListAsync(ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PrescriptionResponseDto>> Get(int id, CancellationToken ct)
    {
        var p = await db.Prescriptions.Include(x => x.PrescriptionItems).ThenInclude(i => i.Medicine)
            .Include(x => x.Customer).FirstOrDefaultAsync(x => x.PrescriptionId == id, ct);
        if (p is null) return NotFound();
        return Ok(Map(p));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult<PrescriptionResponseDto>> Create([FromBody] PrescriptionCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0) return BadRequest("At least one item required.");
        var customer = await db.Customers.FindAsync([dto.CustomerId], ct);
        if (customer is null || !customer.IsActive) return BadRequest("Customer not found or inactive.");

        var medIds = dto.Items.Select(i => i.MedicineId).Distinct().ToList();
        var meds = await db.Medicines.Where(m => medIds.Contains(m.MedicineId) && m.IsActive).ToDictionaryAsync(m => m.MedicineId, ct);
        if (meds.Count != medIds.Count) return BadRequest("One or more medicines not found or inactive.");
        foreach (var i in dto.Items)
        {
            if (i.Quantity <= 0) return BadRequest("Quantity must be > 0.");
            if (string.IsNullOrWhiteSpace(i.Dosage)) return BadRequest("Dosage required for each item.");
        }

        var p = new Prescription
        {
            PrescriptionNumber = $"RX-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PrescriptionDate = DateTime.UtcNow,
            CustomerId = dto.CustomerId,
            DoctorName = dto.DoctorName,
            Diagnosis = dto.Diagnosis,
            Notes = dto.Notes
        };
        db.Prescriptions.Add(p);
        await db.SaveChangesAsync(ct);

        foreach (var i in dto.Items)
            db.PrescriptionItems.Add(new PrescriptionItem
            {
                PrescriptionId = p.PrescriptionId, MedicineId = i.MedicineId,
                Dosage = i.Dosage, Quantity = i.Quantity, Duration = i.Duration, Instructions = i.Instructions
            });
        await db.SaveChangesAsync(ct);

        var created = await db.Prescriptions.Include(x => x.PrescriptionItems).ThenInclude(i => i.Medicine)
            .Include(x => x.Customer).FirstAsync(x => x.PrescriptionId == p.PrescriptionId, ct);
        return CreatedAtAction(nameof(Get), new { id = p.PrescriptionId }, Map(created));
    }

    /// <summary>Fulfill = dispense + auto-create Sale + decrement stock (one transaction).</summary>
    [HttpPost("{id:int}/fulfill")]
    [Authorize(Roles = "Admin,Manager,Pharmacist,Cashier")]
    public async Task<ActionResult> Fulfill(int id, [FromBody] PrescriptionFulfillDto dto, CancellationToken ct)
    {
        var p = await db.Prescriptions.Include(x => x.PrescriptionItems).ThenInclude(i => i.Medicine)
            .Include(x => x.Customer).FirstOrDefaultAsync(x => x.PrescriptionId == id, ct);
        if (p is null) return NotFound();
        if (p.IsFulfilled) return BadRequest("Prescription already fulfilled.");

        foreach (var i in p.PrescriptionItems)
        {
            if (!i.Medicine.IsActive) return BadRequest($"Medicine '{i.Medicine.MedicineName}' is inactive.");
            if (i.Medicine.StockQuantity < i.Quantity)
                return BadRequest($"Insufficient stock for '{i.Medicine.MedicineName}'. Available: {i.Medicine.StockQuantity}, needed: {i.Quantity}.");
        }

        var total = p.PrescriptionItems.Sum(i => i.Quantity * i.Medicine.SalePrice);
        var net = total - dto.DiscountAmount;
        if (net < 0) return BadRequest("Discount exceeds total.");
        if (dto.PaidAmount < net) return BadRequest($"Paid ({dto.PaidAmount}) less than net ({net}).");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var sale = new Sale
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            SaleDate = DateTime.UtcNow,
            CustomerId = p.CustomerId,
            UserId = CurrentUserId,
            TotalAmount = total,
            DiscountAmount = dto.DiscountAmount,
            NetAmount = net,
            PaidAmount = dto.PaidAmount,
            ChangeAmount = dto.PaidAmount - net,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod,
            Notes = $"Prescription {p.PrescriptionNumber}"
        };
        db.Sales.Add(sale);
        await db.SaveChangesAsync(ct);

        foreach (var i in p.PrescriptionItems)
        {
            db.SaleItems.Add(new SaleItem { SaleId = sale.SaleId, MedicineId = i.MedicineId, Quantity = i.Quantity, UnitPrice = i.Medicine.SalePrice, TotalPrice = i.Quantity * i.Medicine.SalePrice });
            i.Medicine.StockQuantity -= i.Quantity;
            i.Medicine.UpdatedDate = DateTime.UtcNow;
        }
        p.Customer.TotalPurchase += net;
        p.IsFulfilled = true;
        p.FulfilledDate = DateTime.UtcNow;
        p.FulfilledByUserId = CurrentUserId;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new { saleId = sale.SaleId, invoiceNumber = sale.InvoiceNumber, net, prescriptionId = p.PrescriptionId, prescriptionNumber = p.PrescriptionNumber });
    }

    private static PrescriptionResponseDto Map(Prescription p) => new(
        p.PrescriptionId, p.PrescriptionNumber, p.PrescriptionDate, p.CustomerId, p.Customer?.CustomerName,
        p.DoctorName, p.Diagnosis, p.IsFulfilled, p.FulfilledDate,
        p.PrescriptionItems.Select(i => new PrescriptionItemResponseDto(i.MedicineId, i.Medicine.MedicineName, i.Dosage, i.Quantity, i.Duration, i.Instructions)).ToList());
}
