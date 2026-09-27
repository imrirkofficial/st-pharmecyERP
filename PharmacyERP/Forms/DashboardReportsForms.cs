using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

public class DashboardForm : Form
{
    private readonly Label lblStats = new() { Dock = DockStyle.Top, Height = 90, Font = new Font("Segoe UI", 12F), Padding = new Padding(12) };
    private readonly DataGridView gridLow = new() { Dock = DockStyle.Left, Width = 480, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridRecent = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    public DashboardForm()
    {
        Text = "Dashboard";
        Controls.Add(gridRecent); Controls.Add(gridLow); Controls.Add(lblStats);
        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var d = await ApiClient.Instance.GetAsync<DashboardDto>("api/reports/dashboard");
            if (d is null) return;
            lblStats.Text = $"Today: {d.TodaySales:0.00} ({d.TodayInvoices} invoices)   |   Month: {d.MonthSales:0.00}   |   Customers: {d.TotalCustomers}   |   Suppliers: {d.TotalSuppliers}";
            gridLow.DataSource = d.LowStock.Select(x => new { x.MedicineId, x.MedicineName, x.StockQuantity, x.ReorderLevel }).ToList();
            gridRecent.DataSource = d.Recent.Select(x => new { x.InvoiceNumber, x.SaleDate, x.NetAmount, x.PaymentMethod }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Dashboard failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

public class ReportsForm : Form
{
    private readonly DateTimePicker dtDate = new() { Width = 150 };
    private readonly Label lblDaily = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };
    private readonly Label lblProfit = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };
    private readonly Label lblStock = new() { AutoSize = true, Font = new Font("Segoe UI", 11F) };

    public ReportsForm()
    {
        Text = "Reports";
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(16), AutoScroll = true };
        var btnLoad = new Button { Text = "Load Reports", Width = 130 };
        btnLoad.Click += async (_, _) => await LoadAsync();
        p.Controls.AddRange([new Label { Text = "Date", AutoSize = true }, dtDate, btnLoad, lblDaily, lblProfit, lblStock]);
        Controls.Add(p);
        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var date = dtDate.Value.ToString("yyyy-MM-dd");
            var daily = await ApiClient.Instance.GetAsync<DailySalesDto>($"api/reports/daily-sales?date={date}");
            var profit = await ApiClient.Instance.GetAsync<ProfitDto>($"api/reports/profit?from={date}&to={date}");
            var stock = await ApiClient.Instance.GetAsync<StockReportDto>("api/reports/stock");
            if (daily is not null) lblDaily.Text = $"Daily [{daily.Date}]: invoices={daily.InvoiceCount}, revenue={daily.NetRevenue:0.00}, items={daily.ItemsSold}";
            if (profit is not null) lblProfit.Text = $"Profit [{profit.From}]: revenue={profit.Revenue:0.00}, COGS={profit.Cogs:0.00}, profit={profit.Profit:0.00}";
            if (stock is not null) lblStock.Text = $"Stock: total={stock.TotalMedicines}, low={stock.LowStockCount}, out={stock.OutOfStockCount}, exp30={stock.Expiring30dCount}, value(buy)={stock.StockPurchaseValue:0.00}";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Reports failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
