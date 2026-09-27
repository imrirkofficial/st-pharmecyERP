using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyERP2.Infrastructure.Data;

namespace PharmacyERP2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController(PharmacyDbContext db) : ControllerBase
{
    [HttpGet("daily-sales")]
    public async Task<ActionResult> DailySales([FromQuery] DateTime? date, CancellationToken ct)
    {
        var d = (date ?? DateTime.UtcNow).Date;
        var next = d.AddDays(1);
        var q = db.Sales.Where(s => s.SaleDate >= d && s.SaleDate < next);
        return Ok(new
        {
            date = d.ToString("yyyy-MM-dd"),
            invoiceCount = await q.CountAsync(ct),
            totalAmount = await q.SumAsync(s => (decimal?)s.TotalAmount, ct) ?? 0,
            discount = await q.SumAsync(s => (decimal?)s.DiscountAmount, ct) ?? 0,
            netRevenue = await q.SumAsync(s => (decimal?)s.NetAmount, ct) ?? 0,
            itemsSold = await db.SaleItems.Where(i => i.Sale.SaleDate >= d && i.Sale.SaleDate < next).SumAsync(i => (int?)i.Quantity, ct) ?? 0
        });
    }

    [HttpGet("stock")]
    public async Task<ActionResult> Stock(CancellationToken ct)
    {
        var active = db.Medicines.Where(m => m.IsActive);
        return Ok(new
        {
            totalMedicines = await active.CountAsync(ct),
            lowStockCount = await active.CountAsync(m => m.StockQuantity <= m.ReorderLevel, ct),
            outOfStockCount = await active.CountAsync(m => m.StockQuantity == 0, ct),
            expiring30dCount = await active.CountAsync(m => m.ExpiryDate != null && m.ExpiryDate <= DateTime.UtcNow.AddDays(30), ct),
            stockPurchaseValue = await active.SumAsync(m => (decimal?)(m.StockQuantity * m.PurchasePrice), ct) ?? 0,
            stockSaleValue = await active.SumAsync(m => (decimal?)(m.StockQuantity * m.SalePrice), ct) ?? 0
        });
    }

    [HttpGet("profit")]
    public async Task<ActionResult> Profit([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var f = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
        var t = ((to ?? DateTime.UtcNow).Date).AddDays(1);
        var items = await db.SaleItems.Include(i => i.Medicine)
            .Where(i => i.Sale.SaleDate >= f && i.Sale.SaleDate < t).ToListAsync(ct);
        var revenue = await db.Sales.Where(s => s.SaleDate >= f && s.SaleDate < t).SumAsync(s => (decimal?)s.NetAmount, ct) ?? 0;
        var cogs = items.Sum(i => i.Quantity * i.Medicine.PurchasePrice);
        return Ok(new { from = f.ToString("yyyy-MM-dd"), to = t.AddDays(-1).ToString("yyyy-MM-dd"), revenue, cogs, profit = revenue - cogs, itemsSold = items.Sum(i => i.Quantity), invoices = await db.Sales.CountAsync(s => s.SaleDate >= f && s.SaleDate < t, ct) });
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> Dashboard(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todaySales = await db.Sales.Where(s => s.SaleDate >= today).SumAsync(s => (decimal?)s.NetAmount, ct) ?? 0;
        var monthSales = await db.Sales.Where(s => s.SaleDate >= monthStart).SumAsync(s => (decimal?)s.NetAmount, ct) ?? 0;
        var lowStock = await db.Medicines.AsNoTracking().Where(m => m.IsActive && m.StockQuantity <= m.ReorderLevel)
            .OrderBy(m => m.StockQuantity).Take(10).Select(m => new { m.MedicineId, m.MedicineName, m.StockQuantity, m.ReorderLevel }).ToListAsync(ct);
        var expiring = await db.Medicines.AsNoTracking().Where(m => m.IsActive && m.ExpiryDate != null && m.ExpiryDate <= DateTime.UtcNow.AddDays(30))
            .OrderBy(m => m.ExpiryDate).Take(10).Select(m => new { m.MedicineId, m.MedicineName, m.ExpiryDate, m.StockQuantity }).ToListAsync(ct);
        var recent = await db.Sales.AsNoTracking().OrderByDescending(s => s.SaleDate).Take(5)
            .Select(s => new { s.SaleId, s.InvoiceNumber, s.SaleDate, s.NetAmount, s.PaymentMethod }).ToListAsync(ct);
        var topSelling = await db.SaleItems.GroupBy(i => new { i.MedicineId, i.Medicine.MedicineName })
            .Select(g => new { medicineId = g.Key.MedicineId, medicineName = g.Key.MedicineName, qty = g.Sum(i => i.Quantity), revenue = g.Sum(i => i.TotalPrice) })
            .OrderByDescending(x => x.qty).Take(5).ToListAsync(ct);
        return Ok(new
        {
            todaySales, monthSales,
            todayInvoices = await db.Sales.CountAsync(s => s.SaleDate >= today, ct),
            totalCustomers = await db.Customers.CountAsync(c => c.IsActive, ct),
            totalSuppliers = await db.Suppliers.CountAsync(s => s.IsActive, ct),
            lowStock, expiring, recent, topSelling
        });
    }
}
