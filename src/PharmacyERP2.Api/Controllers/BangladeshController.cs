using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyERP2.Application.DTOs;
using PharmacyERP2.Core.Entities;
using PharmacyERP2.Core.Enums;
using PharmacyERP2.Infrastructure.Data;

namespace PharmacyERP2.Api.Controllers;

/// <summary>Bangladesh retail module: branches, doctors, due khata, supplier
/// payments, returns, closings, audit, SMS outbox, VAT, reorder, backup.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BangladeshController(PharmacyDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private void Audit(string action, string? entity, int? entityId, string? details)
        => db.AuditLogs.Add(new AuditLog
        { UserId = CurrentUserId, Action = action, Entity = entity, EntityId = entityId, Details = details });

    // ── Branches (Single mode = just "Main Branch") ──
    [HttpGet("branches")]
    public async Task<ActionResult> Branches(CancellationToken ct)
    {
        if (!await db.Branches.AnyAsync(ct))
        {
            db.Branches.Add(new Branch { BranchName = "Main Branch", IsHeadOffice = true });
            await db.SaveChangesAsync(ct);
        }
        var items = await db.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync(ct);
        return Ok(items.Select(b => new BranchDto(b.BranchId, b.BranchName, b.Address, b.Phone, b.IsHeadOffice, b.IsActive)));
    }

    [HttpPost("branches")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> CreateBranch([FromBody] BranchCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.BranchName)) return BadRequest("BranchName required.");
        var b = new Branch
        {
            BranchName = dto.BranchName.Trim(), Address = dto.Address,
            Phone = dto.Phone, IsHeadOffice = dto.IsHeadOffice
        };
        db.Branches.Add(b);
        Audit("Branch.Create", "Branch", null, b.BranchName);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Branches), new BranchDto(b.BranchId, b.BranchName, b.Address, b.Phone, b.IsHeadOffice, b.IsActive));
    }

    // ── Doctors ──
    [HttpGet("doctors")]
    public async Task<ActionResult> Doctors([FromQuery] string? search, CancellationToken ct)
    {
        var q = db.Doctors.AsNoTracking().Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(d => d.DoctorName.Contains(search));
        var items = await q.OrderBy(d => d.DoctorName).Take(100).ToListAsync(ct);
        return Ok(items.Select(d => new DoctorDto(d.DoctorId, d.DoctorName, d.Chamber, d.Phone, d.CommissionPercent)));
    }

    [HttpPost("doctors")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> CreateDoctor([FromBody] DoctorCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.DoctorName)) return BadRequest("DoctorName required.");
        var d = new Doctor
        {
            DoctorName = dto.DoctorName.Trim(), Chamber = dto.Chamber,
            Phone = dto.Phone, CommissionPercent = dto.CommissionPercent
        };
        db.Doctors.Add(d);
        Audit("Doctor.Create", "Doctor", null, d.DoctorName);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Doctors), new DoctorDto(d.DoctorId, d.DoctorName, d.Chamber, d.Phone, d.CommissionPercent));
    }

    [HttpPost("prescriptions/link-doctor")]
    [Authorize(Roles = "Admin,Manager,Pharmacist,Cashier")]
    public async Task<ActionResult> LinkDoctor([FromBody] RxLinkDto dto, CancellationToken ct)
    {
        var rx = await db.Prescriptions.FindAsync([dto.PrescriptionId], ct);
        if (rx is null) return NotFound("Prescription not found.");
        var doc = await db.Doctors.FindAsync([dto.DoctorId], ct);
        if (doc is null || !doc.IsActive) return BadRequest("Doctor not found.");
        rx.DoctorId = doc.DoctorId;
        rx.DoctorName = doc.DoctorName;
        if (!string.IsNullOrWhiteSpace(dto.ImagePath)) rx.ImagePath = dto.ImagePath;
        Audit("Rx.LinkDoctor", "Prescription", rx.PrescriptionId, doc.DoctorName);
        await db.SaveChangesAsync(ct);
        return Ok(new { rx.PrescriptionId, Doctor = doc.DoctorName, rx.ImagePath });
    }

    // ── Due Khata ──
    [HttpGet("dues")]
    public async Task<ActionResult> Dues([FromQuery] bool onlyPending = true, CancellationToken ct = default)
    {
        var q = db.Customers.AsNoTracking().Where(c => c.IsActive);
        if (onlyPending) q = q.Where(c => c.DueBalance > 0);
        var items = await q.OrderByDescending(c => c.DueBalance).Take(200)
            .Select(c => new { c.CustomerId, c.CustomerName, c.Phone, c.DueBalance, c.TotalPurchase }).ToListAsync(ct);
        var totalDue = await db.Customers.Where(c => c.IsActive).SumAsync(c => (decimal?)c.DueBalance, ct) ?? 0;
        return Ok(new { totalDue, items });
    }

    [HttpGet("dues/{customerId:int}/ledger")]
    public async Task<ActionResult> DueLedger(int customerId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.CustomerId == customerId, ct)) return NotFound("Customer not found.");
        var txns = await db.DueTransactions.AsNoTracking().Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.TxnDate).Take(100)
            .Select(t => new { t.DueTransactionId, t.SaleId, t.Amount, t.Type, t.Method, t.TxnDate, t.Notes }).ToListAsync(ct);
        return Ok(txns);
    }

    [HttpPost("dues/collect")]
    [Authorize(Roles = "Admin,Manager,Pharmacist,Cashier")]
    public async Task<ActionResult> CollectDue([FromBody] DueCollectDto dto, CancellationToken ct)
    {
        if (dto.Amount <= 0) return BadRequest("Amount must be > 0.");
        var c = await db.Customers.FindAsync([dto.CustomerId], ct);
        if (c is null) return BadRequest("Customer not found.");
        if (dto.Amount > c.DueBalance) return BadRequest($"Amount ({dto.Amount}) exceeds due ({c.DueBalance}).");
        c.DueBalance -= dto.Amount;
        db.DueTransactions.Add(new DueTransaction
        {
            CustomerId = c.CustomerId, Amount = -dto.Amount, Type = "Collect",
            Method = string.IsNullOrWhiteSpace(dto.Method) ? "Cash" : dto.Method,
            UserId = CurrentUserId, Notes = dto.Notes
        });
        Audit("Due.Collect", "Customer", c.CustomerId, $"Collected {dto.Amount}, remaining {c.DueBalance}");
        await db.SaveChangesAsync(ct);
        return Ok(new { c.CustomerId, c.CustomerName, RemainingDue = c.DueBalance });
    }

    // ── Supplier payments ──
    [HttpGet("suppliers/{id:int}/ledger")]
    public async Task<ActionResult> SupplierLedger(int id, CancellationToken ct)
    {
        if (!await db.Suppliers.AnyAsync(s => s.SupplierId == id, ct)) return NotFound("Supplier not found.");
        var pays = await db.SupplierPayments.AsNoTracking().Where(p => p.SupplierId == id)
            .OrderByDescending(p => p.PayDate).Take(100).ToListAsync(ct);
        return Ok(pays.Select(p => new { p.SupplierPaymentId, p.PurchaseId, p.Amount, p.Method, p.TrxId, p.PayDate, p.Notes }));
    }

    [HttpPost("suppliers/{id:int}/pay")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> PaySupplier(int id, [FromBody] SupplierPayDto dto, CancellationToken ct)
    {
        if (dto.Amount <= 0) return BadRequest("Amount must be > 0.");
        var s = await db.Suppliers.FindAsync([id], ct);
        if (s is null) return BadRequest("Supplier not found.");
        if (dto.Amount > s.OutstandingBalance)
            return BadRequest($"Amount ({dto.Amount}) exceeds outstanding ({s.OutstandingBalance}).");
        Purchase? po = null;
        if (dto.PurchaseId.HasValue)
        {
            po = await db.Purchases.FindAsync([dto.PurchaseId.Value], ct);
            if (po is null || po.SupplierId != id) return BadRequest("Purchase not found for this supplier.");
            var apply = Math.Min(po.DueAmount, dto.Amount);
            po.DueAmount -= apply;
            po.PaidAmount += apply;
            po.PaymentStatus = po.DueAmount == 0 ? PaymentStatus.Paid : PaymentStatus.Partial;
        }
        s.OutstandingBalance -= dto.Amount;
        db.SupplierPayments.Add(new SupplierPayment
        {
            SupplierId = id, PurchaseId = dto.PurchaseId, Amount = dto.Amount,
            Method = dto.Method ?? "Cash", TrxId = dto.TrxId, UserId = CurrentUserId, Notes = dto.Notes
        });
        Audit("Supplier.Pay", "Supplier", id, $"Paid {dto.Amount}, remaining {s.OutstandingBalance}");
        await db.SaveChangesAsync(ct);
        return Ok(new { s.SupplierId, s.SupplierName, Remaining = s.OutstandingBalance });
    }

    // ── Returns ──
    [HttpPost("returns/sale")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> SaleReturn([FromBody] SaleReturnCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0) return BadRequest("Items required.");
        var sale = await db.Sales.Include(s => s.SaleItems).Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.SaleId == dto.SaleId, ct);
        if (sale is null) return BadRequest("Sale not found.");

        var medIds = dto.Items.Select(i => i.MedicineId).Distinct().ToList();
        var meds = await db.Medicines.Where(m => medIds.Contains(m.MedicineId)).ToDictionaryAsync(m => m.MedicineId, ct);
        if (meds.Count != medIds.Count) return BadRequest("Medicine not found.");
        var returned = await db.SaleReturnItems.AsNoTracking()
            .Where(i => i.SaleReturn.SaleId == dto.SaleId).GroupBy(i => i.MedicineId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Quantity), ct);

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var ret = new SaleReturn
        {
            ReturnNumber = $"SR-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            SaleId = sale.SaleId, UserId = CurrentUserId, BranchId = sale.BranchId,
            Reason = dto.Reason, ReturnDate = DateTime.UtcNow
        };
        db.SaleReturns.Add(ret);
        await db.SaveChangesAsync(ct);

        decimal refund = 0;
        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0) return BadRequest("Quantity must be > 0.");
            var sold = sale.SaleItems.Where(i => i.MedicineId == item.MedicineId).Sum(i => i.Quantity);
            var prev = returned.TryGetValue(item.MedicineId, out var r) ? r : 0;
            if (item.Quantity > sold - prev)
                return BadRequest($"Medicine {item.MedicineId}: sold {sold}, already returned {prev}.");
            var price = sale.SaleItems.First(i => i.MedicineId == item.MedicineId).UnitPrice;
            db.SaleReturnItems.Add(new SaleReturnItem
            { SaleReturnId = ret.SaleReturnId, MedicineId = item.MedicineId, Quantity = item.Quantity, UnitPrice = price });
            meds[item.MedicineId].StockQuantity += item.Quantity;
            refund += item.Quantity * price;
        }
        ret.RefundAmount = refund;
        if (sale.Customer is not null)
        {
            sale.Customer.TotalPurchase = Math.Max(0, sale.Customer.TotalPurchase - refund);
            sale.Customer.DueBalance = Math.Max(0, sale.Customer.DueBalance - Math.Min(refund, sale.Customer.DueBalance));
        }
        Audit("Sale.Return", "Sale", sale.SaleId, $"{ret.ReturnNumber} Refund={refund}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { ret.SaleReturnId, ret.ReturnNumber, Refund = refund });
    }

    [HttpPost("returns/purchase")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> PurchaseReturn([FromBody] PurchaseReturnCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0) return BadRequest("Items required.");
        if (dto.Reason is not ("Expired" or "Damaged" or "Other")) return BadRequest("Reason must be Expired/Damaged/Other.");
        var sup = await db.Suppliers.FindAsync([dto.SupplierId], ct);
        if (sup is null) return BadRequest("Supplier not found.");

        var medIds = dto.Items.Select(i => i.MedicineId).Distinct().ToList();
        var meds = await db.Medicines.Where(m => medIds.Contains(m.MedicineId)).ToDictionaryAsync(m => m.MedicineId, ct);
        if (meds.Count != medIds.Count) return BadRequest("Medicine not found.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var ret = new PurchaseReturn
        {
            ReturnNumber = $"PR-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PurchaseId = dto.PurchaseId, SupplierId = dto.SupplierId,
            UserId = CurrentUserId, Reason = dto.Reason, ReturnDate = DateTime.UtcNow
        };
        db.PurchaseReturns.Add(ret);
        await db.SaveChangesAsync(ct);

        decimal amount = 0;
        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0) return BadRequest("Quantity must be > 0.");
            var med = meds[item.MedicineId];
            if (med.StockQuantity < item.Quantity)
                return BadRequest($"'{med.MedicineName}' stock only {med.StockQuantity}.");
            med.StockQuantity -= item.Quantity;
            var price = med.PurchasePrice;
            db.PurchaseReturnItems.Add(new PurchaseReturnItem
            { PurchaseReturnId = ret.PurchaseReturnId, MedicineId = item.MedicineId, Quantity = item.Quantity, UnitPrice = price });
            amount += item.Quantity * price;
        }
        ret.ReturnAmount = amount;
        sup.OutstandingBalance = Math.Max(0, sup.OutstandingBalance - amount);
        if (dto.PurchaseId.HasValue)
        {
            var po = await db.Purchases.FindAsync([dto.PurchaseId.Value], ct);
            if (po is not null && po.SupplierId == dto.SupplierId)
            {
                var cut = Math.Min(po.DueAmount, amount);
                po.DueAmount -= cut;
                po.PaymentStatus = po.DueAmount == 0 && po.PaidAmount > 0 ? PaymentStatus.Paid
                    : po.DueAmount == po.TotalAmount ? PaymentStatus.Pending : PaymentStatus.Partial;
            }
        }
        Audit("Purchase.Return", "Supplier", sup.SupplierId, $"{ret.ReturnNumber} {dto.Reason} Amount={amount}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { ret.PurchaseReturnId, ret.ReturnNumber, Amount = amount });
    }

    [HttpGet("expiry-returns")]
    public async Task<ActionResult> ExpiryReturns([FromQuery] int days = 90, CancellationToken ct = default)
    {
        var limit = DateTime.UtcNow.Date.AddDays(days);
        var items = await db.Medicines.AsNoTracking()
            .Where(m => m.IsActive && m.ExpiryDate != null && m.ExpiryDate <= limit && m.StockQuantity > 0)
            .OrderBy(m => m.ExpiryDate).Take(200)
            .Select(m => new
            {
                m.MedicineId, m.MedicineName, m.Manufacturer, m.ExpiryDate,
                m.StockQuantity, m.PurchasePrice, StockValue = m.StockQuantity * m.PurchasePrice
            }).ToListAsync(ct);
        return Ok(new { days, count = items.Count, items });
    }

    // ── Day closing ──
    [HttpGet("closings")]
    public async Task<ActionResult> Closings([FromQuery] int take = 30, CancellationToken ct = default)
    {
        var items = await db.DayClosings.AsNoTracking().OrderByDescending(c => c.BusinessDate)
            .Take(Math.Clamp(take, 1, 100))
            .Select(c => new
            {
                c.DayClosingId, c.BusinessDate, c.BranchId, c.InvoiceCount, c.TotalSales,
                c.CashSales, c.MobileSales, c.CardSales, c.DueSales, c.DueCollected,
                c.PurchasePaid, c.TotalProfit, c.ClosedAt, c.Notes
            }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("closings")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> CloseDay([FromBody] ClosingCreateDto dto, CancellationToken ct)
    {
        var d = dto.BusinessDate.Date;
        if (await db.DayClosings.AnyAsync(c => c.BusinessDate == d && c.BranchId == dto.BranchId, ct))
            return Conflict("Already closed for this date/branch.");

        var next = d.AddDays(1);
        var sales = db.Sales.Where(s => s.SaleDate >= d && s.SaleDate < next);
        if (dto.BranchId.HasValue) sales = sales.Where(s => s.BranchId == dto.BranchId);
        var saleIds = await sales.Select(s => s.SaleId).ToListAsync(ct);

        var pays = await db.SalePayments.AsNoTracking().Where(p => saleIds.Contains(p.SaleId)).ToListAsync(ct);
        decimal SumP(params string[] ms) => pays.Where(p => ms.Contains(p.Method)).Sum(p => p.Amount);
        var cash = SumP("Cash");
        var mobile = SumP("bKash", "Nagad", "Rocket", "Upay", "Mobile Banking");
        var card = SumP("Card");
        var other = pays.Sum(p => p.Amount) - cash - mobile - card;

        var totalSales = await sales.SumAsync(s => (decimal?)s.NetAmount, ct) ?? 0;
        var dueSales = await sales.SumAsync(s => (decimal?)s.DueAmount, ct) ?? 0;
        var invoices = saleIds.Count;
        var dueCollected = await db.DueTransactions
            .Where(t => t.Type == "Collect" && t.TxnDate >= d && t.TxnDate < next)
            .SumAsync(t => (decimal?)-t.Amount, ct) ?? 0;
        var purchasePaid = await db.SupplierPayments
            .Where(p => p.PayDate >= d && p.PayDate < next)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        var cogs = await db.SaleItems.Include(i => i.Medicine)
            .Where(i => saleIds.Contains(i.SaleId))
            .SumAsync(i => (decimal?)(i.Quantity * i.Medicine.PurchasePrice), ct) ?? 0;

        var closing = new DayClosing
        {
            BusinessDate = d, BranchId = dto.BranchId, UserId = CurrentUserId,
            CashSales = cash + other, MobileSales = mobile, CardSales = card,
            DueSales = dueSales, DueCollected = dueCollected, PurchasePaid = purchasePaid,
            TotalSales = totalSales, TotalProfit = totalSales - cogs,
            InvoiceCount = invoices, Notes = dto.Notes
        };
        db.DayClosings.Add(closing);
        Audit("Day.Close", "DayClosing", null, $"{d:yyyy-MM-dd} Sales={totalSales} Profit={closing.TotalProfit}");
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            closing.DayClosingId, closing.BusinessDate, closing.BranchId, closing.InvoiceCount,
            closing.TotalSales, closing.CashSales, closing.MobileSales, closing.CardSales,
            closing.DueSales, closing.DueCollected, closing.PurchasePaid, closing.TotalProfit
        });
    }

    // ── Audit ──
    [HttpGet("audit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> AuditList([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await db.AuditLogs.CountAsync(ct);
        var items = await db.AuditLogs.AsNoTracking().OrderByDescending(a => a.LogDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new { a.AuditLogId, a.UserId, a.Action, a.Entity, a.EntityId, a.Details, a.LogDate })
            .ToListAsync(ct);
        return Ok(new { total, page, pageSize, items });
    }

    // ── SMS outbox (provider pore: SSL Wireless / BulkSMS BD) ──
    [HttpGet("sms")]
    public async Task<ActionResult> SmsList(CancellationToken ct)
    {
        var items = await db.SmsLogs.AsNoTracking().OrderByDescending(s => s.SmsLogId).Take(100)
            .Select(s => new { s.SmsLogId, s.Phone, s.Message, s.Status, s.CustomerId }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("sms")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> QueueSms([FromBody] SmsCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest("Phone and Message required.");
        var sms = new SmsLog { Phone = dto.Phone.Trim(), Message = dto.Message.Trim(), CustomerId = dto.CustomerId };
        db.SmsLogs.Add(sms);
        await db.SaveChangesAsync(ct);
        return Ok(new { sms.SmsLogId, sms.Status });
    }

    [HttpPost("sms/due-reminders")]
    [Authorize(Roles = "Admin,Manager,Pharmacist")]
    public async Task<ActionResult> DueReminders(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var debtors = await db.Customers
            .Where(c => c.IsActive && c.DueBalance > 0 && c.Phone != null && c.Phone != "")
            .ToListAsync(ct);
        int queued = 0;
        foreach (var c in debtors)
        {
            bool exists = await db.SmsLogs.AnyAsync(s => s.CustomerId == c.CustomerId
                && s.CreatedDate >= today && s.Status == "Queued", ct);
            if (exists) continue;
            db.SmsLogs.Add(new SmsLog
            {
                Phone = c.Phone!,
                Message = $"Priyo {c.CustomerName}, apnar {c.DueBalance:0} TK baki ache. Onugroho kore porishodh korun. - Pharmacy",
                CustomerId = c.CustomerId
            });
            queued++;
        }
        Audit("Sms.DueReminders", "Customer", null, $"Queued={queued}");
        await db.SaveChangesAsync(ct);
        return Ok(new { queued });
    }

    // ── VAT / Mushok report ──
    [HttpGet("reports/vat")]
    public async Task<ActionResult> Vat([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        var f = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
        var t = ((to ?? DateTime.UtcNow).Date).AddDays(1);
        var q = db.Sales.Where(s => s.SaleDate >= f && s.SaleDate < t);
        return Ok(new
        {
            from = f.ToString("yyyy-MM-dd"),
            to = t.AddDays(-1).ToString("yyyy-MM-dd"),
            invoices = await q.CountAsync(ct),
            revenue = await q.SumAsync(s => (decimal?)s.NetAmount, ct) ?? 0,
            vatCollected = await q.SumAsync(s => (decimal?)s.VatAmount, ct) ?? 0,
            dueTotal = await q.SumAsync(s => (decimal?)s.DueAmount, ct) ?? 0
        });
    }

    // ── Auto reorder suggestion ──
    [HttpGet("reorder")]
    public async Task<ActionResult> Reorder(CancellationToken ct)
    {
        var items = await db.Medicines.AsNoTracking()
            .Where(m => m.IsActive && m.StockQuantity <= m.ReorderLevel)
            .OrderBy(m => m.Manufacturer).ThenBy(m => m.MedicineName).Take(200)
            .Select(m => new
            {
                m.MedicineId, m.MedicineName, m.Manufacturer, m.StockQuantity, m.ReorderLevel,
                SuggestedQty = Math.Max(m.ReorderLevel * 2 - m.StockQuantity, m.ReorderLevel),
                m.PurchasePrice
            }).ToListAsync(ct);
        return Ok(items);
    }

    // ── Backup snapshot (WinForms saves JSON; .bak via SSMS note) ──
    [HttpGet("backup/snapshot")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> Snapshot(CancellationToken ct)
    {
        return Ok(new
        {
            generatedAt = DateTime.UtcNow,
            users = await db.Users.CountAsync(ct),
            branches = await db.Branches.CountAsync(ct),
            medicines = await db.Medicines.CountAsync(m => m.IsActive, ct),
            customers = await db.Customers.CountAsync(ct),
            suppliers = await db.Suppliers.CountAsync(ct),
            sales = await db.Sales.CountAsync(ct),
            purchases = await db.Purchases.CountAsync(ct),
            totalDue = await db.Customers.SumAsync(c => (decimal?)c.DueBalance, ct) ?? 0,
            supplierOutstanding = await db.Suppliers.SumAsync(s => (decimal?)s.OutstandingBalance, ct) ?? 0
        });
    }

    // ── Barcode scanner lookup ──
    [HttpGet("barcode/{code}")]
    public async Task<ActionResult> Barcode(string code, CancellationToken ct)
    {
        var m = await db.Medicines.AsNoTracking().FirstOrDefaultAsync(x => x.Barcode == code && x.IsActive, ct);
        if (m is null) return NotFound("Barcode not found.");
        return Ok(new
        {
            m.MedicineId, m.MedicineName, m.GenericName, m.Manufacturer, m.Category,
            m.PurchasePrice, m.SalePrice, m.StockQuantity, m.ReorderLevel, m.Barcode,
            m.ShelfLocation, m.ExpiryDate, m.UnitsPerStrip, m.StripsPerBox, m.IsControlled, m.VatPercent
        });
    }
}
