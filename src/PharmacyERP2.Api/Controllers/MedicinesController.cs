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
public class MedicinesController(PharmacyDbContext db) : ControllerBase
{
    private static MedicineResponseDto Map(Medicine m) => new(
        m.MedicineId, m.MedicineName, m.GenericName, m.Manufacturer, m.Category,
        m.PurchasePrice, m.SalePrice, m.StockQuantity, m.ReorderLevel,
        m.Barcode, m.ShelfLocation, m.ExpiryDate, m.IsActive,
        m.StockQuantity <= m.ReorderLevel,
        m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date <= DateTime.UtcNow.Date.AddDays(30),
        m.UnitsPerStrip, m.StripsPerBox, m.IsControlled, m.VatPercent);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicineResponseDto>>> List(
        [FromQuery] string? search, [FromQuery] string? category,
        [FromQuery] bool? lowStock, [FromQuery] bool? expiring,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.Medicines.AsNoTracking().Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(m => m.MedicineName.Contains(search) || (m.GenericName != null && m.GenericName.Contains(search)) || (m.Barcode != null && m.Barcode == search));
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(m => m.Category == category);
        if (lowStock == true)
            q = q.Where(m => m.StockQuantity <= m.ReorderLevel);
        if (expiring == true)
            q = q.Where(m => m.ExpiryDate != null && m.ExpiryDate <= DateTime.UtcNow.AddDays(30));

        var items = await q.OrderBy(m => m.MedicineName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MedicineResponseDto>> Get(int id, CancellationToken ct)
    {
        var m = await db.Medicines.FindAsync([id], ct);
        if (m is null) return NotFound();
        return Ok(Map(m));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<MedicineResponseDto>>> LowStock(CancellationToken ct)
    {
        var items = await db.Medicines.AsNoTracking()
            .Where(m => m.IsActive && m.StockQuantity <= m.ReorderLevel)
            .OrderBy(m => m.StockQuantity).ToListAsync(ct);
        return Ok(items.Select(Map));
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<IEnumerable<MedicineResponseDto>>> Expiring([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var limit = DateTime.UtcNow.Date.AddDays(days);
        var items = await db.Medicines.AsNoTracking()
            .Where(m => m.IsActive && m.ExpiryDate != null && m.ExpiryDate <= limit)
            .OrderBy(m => m.ExpiryDate).ToListAsync(ct);
        return Ok(items.Select(Map));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult<MedicineResponseDto>> Create([FromBody] MedicineCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.MedicineName)) return BadRequest("MedicineName required.");
        if (dto.PurchasePrice < 0 || dto.SalePrice < 0 || dto.StockQuantity < 0) return BadRequest("Price/stock must be >= 0.");
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && await db.Medicines.AnyAsync(m => m.Barcode == dto.Barcode, ct))
            return Conflict($"Barcode '{dto.Barcode}' already exists.");

        var m = new Medicine
        {
            MedicineName = dto.MedicineName.Trim(),
            GenericName = dto.GenericName, Manufacturer = dto.Manufacturer, Category = dto.Category,
            DosageForm = dto.DosageForm, Strength = dto.Strength, Unit = dto.Unit,
            PurchasePrice = dto.PurchasePrice, SalePrice = dto.SalePrice,
            StockQuantity = dto.StockQuantity, ReorderLevel = dto.ReorderLevel,
            Barcode = dto.Barcode, ShelfLocation = dto.ShelfLocation,
            ManufactureDate = dto.ManufactureDate, ExpiryDate = dto.ExpiryDate, Notes = dto.Notes,
            UnitsPerStrip = dto.UnitsPerStrip <= 0 ? 10 : dto.UnitsPerStrip,
            StripsPerBox = dto.StripsPerBox <= 0 ? 10 : dto.StripsPerBox,
            IsControlled = dto.IsControlled, VatPercent = dto.VatPercent
        };
        db.Medicines.Add(m);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = m.MedicineId }, Map(m));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult<MedicineResponseDto>> Update(int id, [FromBody] MedicineUpdateDto dto, CancellationToken ct)
    {
        var m = await db.Medicines.FindAsync([id], ct);
        if (m is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && dto.Barcode != m.Barcode &&
            await db.Medicines.AnyAsync(x => x.Barcode == dto.Barcode, ct))
            return Conflict($"Barcode '{dto.Barcode}' already exists.");

        m.MedicineName = dto.MedicineName.Trim();
        m.GenericName = dto.GenericName; m.Manufacturer = dto.Manufacturer; m.Category = dto.Category;
        m.DosageForm = dto.DosageForm; m.Strength = dto.Strength; m.Unit = dto.Unit;
        m.PurchasePrice = dto.PurchasePrice; m.SalePrice = dto.SalePrice;
        m.StockQuantity = dto.StockQuantity; m.ReorderLevel = dto.ReorderLevel;
        m.Barcode = dto.Barcode; m.ShelfLocation = dto.ShelfLocation;
        m.ManufactureDate = dto.ManufactureDate; m.ExpiryDate = dto.ExpiryDate;
        m.IsActive = dto.IsActive; m.Notes = dto.Notes; m.UpdatedDate = DateTime.UtcNow;
        m.UnitsPerStrip = dto.UnitsPerStrip <= 0 ? 10 : dto.UnitsPerStrip;
        m.StripsPerBox = dto.StripsPerBox <= 0 ? 10 : dto.StripsPerBox;
        m.IsControlled = dto.IsControlled; m.VatPercent = dto.VatPercent;
        await db.SaveChangesAsync(ct);
        return Ok(Map(m));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var m = await db.Medicines.FindAsync([id], ct);
        if (m is null) return NotFound();
        m.IsActive = false; // soft delete to keep Sale/Purchase history
        m.UpdatedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
