using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyERP2.Application.DTOs;
using PharmacyERP2.Core.Entities;
using PharmacyERP2.Core.Enums;
using PharmacyERP2.Infrastructure.Data;

namespace PharmacyERP2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController(PharmacyDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Purchases.Include(p => p.Supplier).AsNoTracking().OrderByDescending(p => p.PurchaseDate);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { p.PurchaseId, p.PurchaseNumber, p.PurchaseDate, p.SupplierId, SupplierName = p.Supplier.SupplierName, p.TotalAmount, p.PaidAmount, p.DueAmount, p.PaymentStatus })
            .ToListAsync(ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseResponseDto>> Get(int id, CancellationToken ct)
    {
        var p = await db.Purchases.Include(x => x.PurchaseItems).ThenInclude(i => i.Medicine)
            .Include(x => x.Supplier).FirstOrDefaultAsync(x => x.PurchaseId == id, ct);
        if (p is null) return NotFound();
        return Ok(new PurchaseResponseDto(p.PurchaseId, p.PurchaseNumber, p.PurchaseDate, p.SupplierId, p.Supplier?.SupplierName,
            p.TotalAmount, p.PaidAmount, p.DueAmount, p.PaymentStatus,
            p.PurchaseItems.Select(i => new PurchaseItemResponseDto(i.MedicineId, i.Medicine.MedicineName, i.Quantity, i.UnitPrice, i.TotalPrice, i.BatchNumber)).ToList()));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult<PurchaseResponseDto>> Receive([FromBody] PurchaseCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0) return BadRequest("At least one item required.");
        var supplier = await db.Suppliers.FindAsync([dto.SupplierId], ct);
        if (supplier is null || !supplier.IsActive) return BadRequest("Supplier not found or inactive.");

        var medIds = dto.Items.Select(i => i.MedicineId).Distinct().ToList();
        var meds = await db.Medicines.Where(m => medIds.Contains(m.MedicineId) && m.IsActive).ToDictionaryAsync(m => m.MedicineId, ct);
        if (meds.Count != medIds.Count) return BadRequest("One or more medicines not found or inactive.");
        foreach (var i in dto.Items)
            if (i.Quantity <= 0 || i.UnitPrice < 0) return BadRequest("Quantity > 0 and UnitPrice >= 0 required.");

        var total = dto.Items.Sum(i => i.Quantity * i.UnitPrice);
        if (dto.PaidAmount < 0 || dto.PaidAmount > total) return BadRequest("PaidAmount must be 0..total.");
        var due = total - dto.PaidAmount;
        var status = due == 0 ? PaymentStatus.Paid : dto.PaidAmount == 0 ? PaymentStatus.Pending : PaymentStatus.Partial;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var p = new Purchase
        {
            PurchaseNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PurchaseDate = DateTime.UtcNow,
            SupplierId = dto.SupplierId,
            UserId = CurrentUserId,
            TotalAmount = total, PaidAmount = dto.PaidAmount, DueAmount = due,
            PaymentStatus = status, Notes = dto.Notes
        };
        db.Purchases.Add(p);
        await db.SaveChangesAsync(ct);

        foreach (var i in dto.Items)
        {
            db.PurchaseItems.Add(new PurchaseItem
            {
                PurchaseId = p.PurchaseId, MedicineId = i.MedicineId, Quantity = i.Quantity,
                UnitPrice = i.UnitPrice, TotalPrice = i.Quantity * i.UnitPrice,
                ExpiryDate = i.ExpiryDate, BatchNumber = i.BatchNumber
            });
            var med = meds[i.MedicineId];
            med.StockQuantity += i.Quantity;
            if (i.ExpiryDate.HasValue && (med.ExpiryDate is null || i.ExpiryDate > med.ExpiryDate))
                med.ExpiryDate = i.ExpiryDate;
            med.UpdatedDate = DateTime.UtcNow;
        }
        supplier.TotalPurchase += total;
        supplier.OutstandingBalance += due;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = p.PurchaseId },
            new PurchaseResponseDto(p.PurchaseId, p.PurchaseNumber, p.PurchaseDate, p.SupplierId, supplier.SupplierName,
                total, p.PaidAmount, due, status,
                dto.Items.Select(i => new PurchaseItemResponseDto(i.MedicineId, meds[i.MedicineId].MedicineName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice, i.BatchNumber)).ToList()));
    }
}
