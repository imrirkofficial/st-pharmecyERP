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
public class SalesController(PharmacyDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Sales.Include(s => s.Customer).AsNoTracking().OrderByDescending(s => s.SaleDate).AsQueryable();
        if (from.HasValue) q = q.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue) q = q.Where(s => s.SaleDate <= to.Value);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new { s.SaleId, s.InvoiceNumber, s.SaleDate, s.CustomerId, CustomerName = s.Customer != null ? s.Customer.CustomerName : null, s.TotalAmount, s.DiscountAmount, s.NetAmount, s.PaidAmount, s.ChangeAmount, s.PaymentMethod })
            .ToListAsync(ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleResponseDto>> Get(int id, CancellationToken ct)
    {
        var s = await db.Sales.Include(x => x.SaleItems).ThenInclude(i => i.Medicine)
            .Include(x => x.Customer).FirstOrDefaultAsync(x => x.SaleId == id, ct);
        if (s is null) return NotFound();
        return Ok(new SaleResponseDto(s.SaleId, s.InvoiceNumber, s.SaleDate, s.CustomerId, s.Customer?.CustomerName,
            s.TotalAmount, s.DiscountAmount, s.NetAmount, s.PaidAmount, s.ChangeAmount, s.PaymentMethod,
            s.SaleItems.Select(i => new SaleItemResponseDto(i.MedicineId, i.Medicine.MedicineName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList()));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Pharmacist,Cashier")]
    public async Task<ActionResult<SaleResponseDto>> Create([FromBody] SaleCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0) return BadRequest("At least one item required.");
        if (dto.DiscountAmount < 0 || dto.PaidAmount < 0) return BadRequest("Discount/Paid must be >= 0.");

        var medIds = dto.Items.Select(i => i.MedicineId).Distinct().ToList();
        var meds = await db.Medicines.Where(m => medIds.Contains(m.MedicineId) && m.IsActive).ToDictionaryAsync(m => m.MedicineId, ct);
        if (meds.Count != medIds.Count) return BadRequest("One or more medicines not found or inactive.");

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0) return BadRequest($"Quantity must be > 0 (medicine {item.MedicineId}).");
            if (meds[item.MedicineId].StockQuantity < item.Quantity)
                return BadRequest($"Insufficient stock for '{meds[item.MedicineId].MedicineName}'. Available: {meds[item.MedicineId].StockQuantity}, requested: {item.Quantity}.");
        }

        Customer? customer = null;
        if (dto.CustomerId.HasValue)
        {
            customer = await db.Customers.FindAsync([dto.CustomerId.Value], ct);
            if (customer is null) return BadRequest("Customer not found.");
        }

        var total = dto.Items.Sum(i => i.Quantity * (i.UnitPrice ?? meds[i.MedicineId].SalePrice));
        var net = total - dto.DiscountAmount;
        if (net < 0) return BadRequest("Discount exceeds total.");
        if (dto.PaidAmount < net) return BadRequest($"Paid ({dto.PaidAmount}) less than net ({net}).");
        var change = dto.PaidAmount - net;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var sale = new Sale
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            SaleDate = DateTime.UtcNow,
            CustomerId = dto.CustomerId,
            UserId = CurrentUserId,
            TotalAmount = total,
            DiscountAmount = dto.DiscountAmount,
            NetAmount = net,
            PaidAmount = dto.PaidAmount,
            ChangeAmount = change,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod,
            Notes = dto.Notes
        };
        db.Sales.Add(sale);
        await db.SaveChangesAsync(ct);

        foreach (var item in dto.Items)
        {
            var med = meds[item.MedicineId];
            var price = item.UnitPrice ?? med.SalePrice;
            db.SaleItems.Add(new SaleItem { SaleId = sale.SaleId, MedicineId = med.MedicineId, Quantity = item.Quantity, UnitPrice = price, TotalPrice = item.Quantity * price });
            med.StockQuantity -= item.Quantity;
            med.UpdatedDate = DateTime.UtcNow;
        }
        if (customer is not null) customer.TotalPurchase += net;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = sale.SaleId },
            new SaleResponseDto(sale.SaleId, sale.InvoiceNumber, sale.SaleDate, sale.CustomerId, customer?.CustomerName,
                total, sale.DiscountAmount, net, sale.PaidAmount, change, sale.PaymentMethod,
                dto.Items.Select(i => new SaleItemResponseDto(i.MedicineId, meds[i.MedicineId].MedicineName, i.Quantity, i.UnitPrice ?? meds[i.MedicineId].SalePrice, i.Quantity * (i.UnitPrice ?? meds[i.MedicineId].SalePrice))).ToList()));
    }
}
