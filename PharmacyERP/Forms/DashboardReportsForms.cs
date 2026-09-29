using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

// ── Modern dashboard: stat cards + low stock + expiry + due + top selling ──
public class DashboardForm : Form
{
    private readonly FlowLayoutPanel cards = new() { Dock = DockStyle.Top, Height = 104, Padding = new Padding(10), AutoScroll = true };
    private readonly DataGridView gridLow = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridExp = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridRecent = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridTop = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    public DashboardForm()
    {
        Text = Lang.T("dashboard");
        var mid = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520 };
        var midL = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250 };
        var midR = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250 };
        midL.Panel1.Controls.Add(Wrap(gridLow, Lang.T("lowStock")));
        midL.Panel2.Controls.Add(Wrap(gridRecent, Lang.T("recentSales")));
        midR.Panel1.Controls.Add(Wrap(gridExp, Lang.T("expirySoon")));
        midR.Panel2.Controls.Add(Wrap(gridTop, Lang.T("topSelling")));
        mid.Panel1.Controls.Add(midL); mid.Panel2.Controls.Add(midR);
        Controls.Add(mid); Controls.Add(cards);
        Shown += async (_, _) => { Theme.Apply(this); await LoadAsync(); };
    }

    private static Panel Wrap(Control grid, string title)
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        var lbl = new Label { Text = title, Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        p.Controls.Add(grid); p.Controls.Add(lbl);
        return p;
    }

    private Panel Card(string title, string value, Color back)
    {
        var p = new Panel { Width = 190, Height = 80, BackColor = back, Margin = new Padding(4) };
        var t = new Label { Text = title, Dock = DockStyle.Top, Height = 26, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };
        var v = new Label { Text = value, Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        p.Controls.Add(v); p.Controls.Add(t);
        return p;
    }

    private async Task LoadAsync()
    {
        try
        {
            var d = await ApiClient.Instance.GetAsync<DashboardDto>("api/reports/dashboard");
            decimal due = 0;
            try { due = (await ApiClient.Instance.GetAsync<DuesDto>("api/bangladesh/dues?onlyPending=true"))?.TotalDue ?? 0; }
            catch { /* dues optional */ }
            if (d is null) return;
            cards.Controls.Clear();
            cards.Controls.AddRange(new Control[]
            {
                Card(Lang.T("todaySales"), $"{d.TodaySales:0.00}", Color.FromArgb(0, 122, 204)),
                Card($"Month", $"{d.MonthSales:0.00}", Color.FromArgb(0, 150, 100)),
                Card(Lang.T("due"), $"{due:0.00}", Color.FromArgb(180, 80, 0)),
                Card("Customers", $"{d.TotalCustomers}", Color.FromArgb(90, 90, 160)),
                Card("Suppliers", $"{d.TotalSuppliers}", Color.FromArgb(100, 100, 100)),
                Card("Invoices", $"{d.TodayInvoices}", Color.FromArgb(0, 120, 140)),
            });
            gridLow.DataSource = d.LowStock.Select(x => new { x.MedicineId, x.MedicineName, x.StockQuantity, x.ReorderLevel }).ToList();
            gridRecent.DataSource = d.Recent.Select(x => new { x.InvoiceNumber, x.SaleDate, x.NetAmount, x.PaymentMethod }).ToList();
            try
            {
                var exp = await ApiClient.Instance.GetAsync<DashboardExtDto>("api/reports/dashboard");
                if (exp is not null)
                {
                    gridExp.DataSource = exp.Expiring.Select(x => new { x.MedicineName, x.ExpiryDate, x.StockQuantity }).ToList();
                    gridTop.DataSource = exp.TopSelling.Select(x => new { x.MedicineName, Qty = x.Qty, Revenue = x.Revenue }).ToList();
                }
            }
            catch { /* extended part optional */ }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Dashboard failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

public class ReportsForm : Form
{
    private readonly DateTimePicker dtFrom = new() { Width = 140 };
    private readonly DateTimePicker dtTo = new() { Width = 140 };
    private readonly Label lblDaily = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };
    private readonly Label lblProfit = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };
    private readonly Label lblStock = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };
    private readonly Label lblVat = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };
    private readonly DataGridView gridReorder = new() { Height = 260, Dock = DockStyle.Bottom, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    public ReportsForm()
    {
        Text = Lang.T("reports");
        dtTo.Value = DateTime.Today;
        dtFrom.Value = DateTime.Today.AddDays(-7);
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(16), AutoScroll = true };
        var btnLoad = new Button { Text = "Load Reports", Width = 130 };
        var btnReorder = new Button { Text = "Auto Reorder", Width = 130 };
        var btnCsv = new Button { Text = "Export Stock CSV", Width = 140 };
        btnLoad.Click += async (_, _) => await LoadAsync();
        btnReorder.Click += async (_, _) => await LoadReorderAsync();
        btnCsv.Click += async (_, _) => await ExportStockCsvAsync();
        p.Controls.AddRange([new Label { Text = "From", AutoSize = true }, dtFrom,
            new Label { Text = "To", AutoSize = true }, dtTo, btnLoad, btnReorder, btnCsv,
            lblDaily, lblProfit, lblStock, lblVat]);
        Controls.Add(gridReorder); Controls.Add(p);
        Shown += async (_, _) => { Theme.Apply(this); await LoadAsync(); };
    }

    private async Task LoadAsync()
    {
        try
        {
            var date = dtTo.Value.ToString("yyyy-MM-dd");
            var from = dtFrom.Value.ToString("yyyy-MM-dd");
            var daily = await ApiClient.Instance.GetAsync<DailySalesDto>($"api/reports/daily-sales?date={date}");
            var profit = await ApiClient.Instance.GetAsync<ProfitDto>($"api/reports/profit?from={from}&to={date}");
            var stock = await ApiClient.Instance.GetAsync<StockReportDto>("api/reports/stock");
            var vat = await ApiClient.Instance.GetAsync<VatDto>($"api/bangladesh/reports/vat?from={from}&to={date}");
            if (daily is not null) lblDaily.Text = $"Daily [{daily.Date}]: invoices={daily.InvoiceCount}, revenue={daily.NetRevenue:0.00}, items={daily.ItemsSold}";
            if (profit is not null) lblProfit.Text = $"Profit [{profit.From}..{profit.To}]: revenue={profit.Revenue:0.00}, COGS={profit.Cogs:0.00}, profit={profit.Profit:0.00}";
            if (stock is not null) lblStock.Text = $"Stock: total={stock.TotalMedicines}, low={stock.LowStockCount}, out={stock.OutOfStockCount}, exp30={stock.Expiring30dCount}, value(buy)={stock.StockPurchaseValue:0.00}";
            if (vat is not null) lblVat.Text = $"Mushok [{vat.From}..{vat.To}]: invoices={vat.Invoices}, revenue={vat.Revenue:0.00}, VAT={vat.VatCollected:0.00}, due={vat.DueTotal:0.00}";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Reports failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task LoadReorderAsync()
    {
        try
        {
            var items = await ApiClient.Instance.GetAsync<List<ReorderDto>>("api/bangladesh/reorder") ?? [];
            gridReorder.DataSource = items.Select(x => new { x.MedicineId, x.MedicineName, x.Manufacturer, x.StockQuantity, x.ReorderLevel, x.SuggestedQty }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Reorder failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task ExportStockCsvAsync()
    {
        try
        {
            var items = await ApiClient.Instance.GetAsync<List<MedicineDto>>("api/medicines?pageSize=200") ?? [];
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"stock-{DateTime.Now:yyyyMMdd}.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var sb = new System.Text.StringBuilder("ID,Name,Generic,Stock,Buy,Sale,Expiry\n");
            foreach (var m in items)
                sb.AppendLine($"{m.MedicineId},\"{m.MedicineName}\",\"{m.GenericName}\",{m.StockQuantity},{m.PurchasePrice:0.00},{m.SalePrice:0.00},{m.ExpiryDate:yyyy-MM-dd}");
            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show("CSV saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
