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
public class SuppliersController(PharmacyDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] string? search, CancellationToken ct)
    {
        var q = db.Suppliers.AsNoTracking().Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.SupplierName.Contains(search) || (s.Phone != null && s.Phone.Contains(search)));
        var items = await q.OrderBy(s => s.SupplierName)
            .Select(s => new SupplierResponseDto(s.SupplierId, s.SupplierName, s.Phone, s.TotalPurchase, s.OutstandingBalance, s.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> Create([FromBody] SupplierCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierName)) return BadRequest("SupplierName required.");
        var s = new Supplier { SupplierName = dto.SupplierName.Trim(), ContactPerson = dto.ContactPerson, Phone = dto.Phone, Email = dto.Email, Address = dto.Address, City = dto.City, Notes = dto.Notes };
        db.Suppliers.Add(s);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = s.SupplierId }, new SupplierResponseDto(s.SupplierId, s.SupplierName, s.Phone, 0, 0, true));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken ct)
    {
        var s = await db.Suppliers.FindAsync([id], ct);
        if (s is null) return NotFound();
        return Ok(new SupplierResponseDto(s.SupplierId, s.SupplierName, s.Phone, s.TotalPurchase, s.OutstandingBalance, s.IsActive));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var s = await db.Suppliers.FindAsync([id], ct);
        if (s is null) return NotFound();
        s.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController(PharmacyDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] string? search, CancellationToken ct)
    {
        var q = db.Customers.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.CustomerName.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
        var items = await q.OrderBy(c => c.CustomerName)
            .Select(c => new CustomerResponseDto(c.CustomerId, c.CustomerName, c.Phone, c.TotalPurchase, c.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Pharmacist,Cashier")]
    public async Task<ActionResult> Create([FromBody] CustomerCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName)) return BadRequest("CustomerName required.");
        var c = new Customer { CustomerName = dto.CustomerName.Trim(), Phone = dto.Phone, Email = dto.Email, Address = dto.Address, MedicalHistory = dto.MedicalHistory, Allergies = dto.Allergies, Notes = dto.Notes };
        db.Customers.Add(c);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = c.CustomerId }, new CustomerResponseDto(c.CustomerId, c.CustomerName, c.Phone, 0, true));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken ct)
    {
        var c = await db.Customers.FindAsync([id], ct);
        if (c is null) return NotFound();
        return Ok(new CustomerResponseDto(c.CustomerId, c.CustomerName, c.Phone, c.TotalPurchase, c.IsActive));
    }
}
