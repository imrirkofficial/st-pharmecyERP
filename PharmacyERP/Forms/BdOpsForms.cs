using System.Text;
using System.Text.Json;
using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

// ── Baki Khata: due list + collect + SMS reminder ──
public class DueKhataForm : Form
{
    private readonly Label lblTotal = new() { Dock = DockStyle.Top, Height = 44, Font = new Font("Segoe UI", 13F, FontStyle.Bold), Padding = new Padding(10, 8, 0, 0) };
    private readonly DataGridView gridDues = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridLedger = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtCustId = new() { Width = 70, PlaceholderText = "Cust ID" };
    private readonly NumericUpDown numAmount = new() { Maximum = 10000000, DecimalPlaces = 2, Width = 120 };
    private readonly ComboBox cmbMethod = new() { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtNotes = new() { Width = 180, PlaceholderText = "Notes" };

    public DueKhataForm()
    {
        Text = Lang.T("dueKhata");
        cmbMethod.Items.AddRange(["Cash", "bKash", "Nagad", "Rocket", "Card"]);
        cmbMethod.SelectedIndex = 0;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        var btnRefresh = new Button { Text = Lang.T("refresh"), Width = 90 };
        var btnSms = new Button { Text = "SMS Reminder", Width = 120 };
        btnRefresh.Click += async (_, _) => await LoadAsync();
        btnSms.Click += async (_, _) => await SmsAsync();
        top.Controls.AddRange([btnRefresh, btnSms]);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };
        split.Panel1.Controls.Add(gridDues);
        split.Panel2.Controls.Add(gridLedger);
        gridDues.SelectionChanged += async (_, _) => await LoadLedgerAsync();

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
        var btnCollect = new Button { Text = Lang.T("collect"), Width = 100, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        btnCollect.Click += async (_, _) => await CollectAsync();
        bottom.Controls.AddRange([new Label { Text = "Cust", AutoSize = true }, txtCustId,
            new Label { Text = Lang.T("paid"), AutoSize = true }, numAmount, cmbMethod, txtNotes, btnCollect]);

        Controls.Add(split); Controls.Add(bottom); Controls.Add(top); Controls.Add(lblTotal);
        Shown += async (_, _) => { Theme.Apply(this); await LoadAsync(); };
    }

    private async Task LoadAsync()
    {
        try
        {
            var d = await ApiClient.Instance.GetAsync<DuesDto>("api/bangladesh/dues?onlyPending=true") ?? new DuesDto(0, []);
            lblTotal.Text = $"{Lang.T("due")}: {d.TotalDue:0.00} BDT ({d.Items.Count})";
            gridDues.DataSource = d.Items.Select(x => new { x.CustomerId, x.CustomerName, x.Phone, Due = x.DueBalance }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task LoadLedgerAsync()
    {
        if (gridDues.CurrentRow is null) return;
        try
        {
            var id = Convert.ToInt32(gridDues.CurrentRow.Cells["CustomerId"].Value);
            txtCustId.Text = id.ToString();
            var items = await ApiClient.Instance.GetAsync<List<LedgerDto>>($"api/bangladesh/dues/{id}/ledger") ?? [];
            gridLedger.DataSource = items.Select(x => new { x.TxnDate, x.Type, x.Amount, x.Method, x.SaleId, x.Notes }).ToList();
        }
        catch { /* ignore selection noise */ }
    }

    private async Task CollectAsync()
    {
        if (!int.TryParse(txtCustId.Text, out var id) || numAmount.Value <= 0) { MessageBox.Show("Customer + amount required."); return; }
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, JsonElement>("api/bangladesh/dues/collect",
                new { customerId = id, amount = numAmount.Value, method = cmbMethod.SelectedItem?.ToString(), notes = txtNotes.Text });
            MessageBox.Show($"Collected. Remaining: {res.GetProperty("remainingDue").GetDecimal():0.00}", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            numAmount.Value = 0; txtNotes.Clear();
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Collect failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task SmsAsync()
    {
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, JsonElement>("api/bangladesh/sms/due-reminders", new { });
            MessageBox.Show($"SMS queued: {res.GetProperty("queued").GetInt32()}", "SMS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "SMS failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

// ── Returns: sale return + purchase/expiry return + expiry list ──
public class ReturnsForm : Form
{
    private readonly DataGridView gridExpiry = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly NumericUpDown numDays = new() { Minimum = 1, Maximum = 365, Value = 90, Width = 70 };
    private readonly TextBox txtSaleId = new() { Width = 70, PlaceholderText = "Sale ID" };
    private readonly TextBox txtMedId = new() { Width = 70, PlaceholderText = "Med ID" };
    private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 70 };
    private readonly TextBox txtReason = new() { Width = 160, PlaceholderText = "Reason" };
    private readonly TextBox txtSupId = new() { Width = 70, PlaceholderText = "Sup ID" };
    private readonly TextBox txtPoId = new() { Width = 70, PlaceholderText = "PO ID (opt)" };
    private readonly TextBox txtMedId2 = new() { Width = 70, PlaceholderText = "Med ID" };
    private readonly NumericUpDown numQty2 = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 70 };
    private readonly ComboBox cmbReason2 = new() { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };

    public ReturnsForm()
    {
        Text = Lang.T("returns");
        cmbReason2.Items.AddRange(["Expired", "Damaged", "Other"]);
        cmbReason2.SelectedIndex = 0;
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var t1 = new TabPage("Sale Return");
        var p1 = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true };
        var btnRet1 = new Button { Text = "Return Sale Item", Width = 140, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        btnRet1.Click += async (_, _) => await SaleReturnAsync();
        p1.Controls.AddRange([new Label { Text = "Sale", AutoSize = true }, txtSaleId, new Label { Text = "Med", AutoSize = true },
            txtMedId, new Label { Text = "Qty", AutoSize = true }, numQty, txtReason, btnRet1]);
        t1.Controls.Add(p1);

        var t2 = new TabPage("Company Return");
        var p2 = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true };
        var btnRet2 = new Button { Text = "Return to Company", Width = 150, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        btnRet2.Click += async (_, _) => await PurchaseReturnAsync();
        p2.Controls.AddRange([new Label { Text = "Supplier", AutoSize = true }, txtSupId, new Label { Text = "PO", AutoSize = true },
            txtPoId, new Label { Text = "Med", AutoSize = true }, txtMedId2, new Label { Text = "Qty", AutoSize = true },
            numQty2, cmbReason2, btnRet2]);
        t2.Controls.Add(p2);

        var t3 = new TabPage("Expiry List");
        var top3 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        var btnLoad = new Button { Text = Lang.T("refresh"), Width = 90 };
        btnLoad.Click += async (_, _) => await LoadExpiryAsync();
        top3.Controls.AddRange([new Label { Text = "Days", AutoSize = true }, numDays, btnLoad]);
        t3.Controls.Add(gridExpiry); t3.Controls.Add(top3);

        tabs.TabPages.AddRange([t1, t2, t3]);
        Controls.Add(tabs);
        Shown += async (_, _) => { Theme.Apply(this); await LoadExpiryAsync(); };
    }

    private async Task SaleReturnAsync()
    {
        if (!int.TryParse(txtSaleId.Text, out var sid) || !int.TryParse(txtMedId.Text, out var mid)) { MessageBox.Show("Sale ID + Med ID required."); return; }
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, JsonElement>("api/bangladesh/returns/sale",
                new { saleId = sid, items = new[] { new { medicineId = mid, quantity = (int)numQty.Value } }, reason = txtReason.Text });
            MessageBox.Show($"Refund: {res.GetProperty("refund").GetDecimal():0.00}\n{res.GetProperty("returnNumber").GetString()}", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Return failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task PurchaseReturnAsync()
    {
        if (!int.TryParse(txtSupId.Text, out var sup) || !int.TryParse(txtMedId2.Text, out var mid)) { MessageBox.Show("Supplier ID + Med ID required."); return; }
        try
        {
            int? po = int.TryParse(txtPoId.Text, out var p) ? p : null;
            var res = await ApiClient.Instance.PostAsync<object, JsonElement>("api/bangladesh/returns/purchase",
                new { supplierId = sup, purchaseId = po, reason = cmbReason2.SelectedItem?.ToString(), items = new[] { new { medicineId = mid, quantity = (int)numQty2.Value } } });
            MessageBox.Show($"Return: {res.GetProperty("amount").GetDecimal():0.00}\n{res.GetProperty("returnNumber").GetString()}", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadExpiryAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Return failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task LoadExpiryAsync()
    {
        try
        {
            var d = await ApiClient.Instance.GetAsync<ExpiryListDto>($"api/bangladesh/expiry-returns?days={(int)numDays.Value}");
            gridExpiry.DataSource = (d?.Items ?? []).Select(x => new { x.MedicineId, x.MedicineName, x.Manufacturer, x.ExpiryDate, x.StockQuantity, Value = x.StockValue }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

// ── Day Closing: din sesh hisab + history + CSV ──
public class ClosingForm : Form
{
    private readonly DateTimePicker dtDate = new() { Width = 150 };
    private readonly Label lblSummary = new() { Dock = DockStyle.Top, Height = 190, Font = new Font("Consolas", 11F), Padding = new Padding(12) };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private ClosingDto? _last;

    public ClosingForm()
    {
        Text = Lang.T("closing");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        var btnClose = new Button { Text = "Close Day", Width = 110, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        var btnRefresh = new Button { Text = Lang.T("refresh"), Width = 90 };
        var btnCsv = new Button { Text = "Export CSV", Width = 100 };
        btnClose.Click += async (_, _) => await CloseAsync();
        btnRefresh.Click += async (_, _) => await LoadAsync();
        btnCsv.Click += (_, _) => ExportCsv();
        top.Controls.AddRange([new Label { Text = "Date", AutoSize = true }, dtDate, btnClose, btnRefresh, btnCsv]);
        Controls.Add(grid); Controls.Add(lblSummary); Controls.Add(top);
        Shown += async (_, _) => { Theme.Apply(this); await LoadAsync(); };
    }

    private async Task LoadAsync()
    {
        try
        {
            var items = await ApiClient.Instance.GetAsync<List<ClosingDto>>("api/bangladesh/closings?take=30") ?? [];
            grid.DataSource = items.Select(x => new { Date = x.BusinessDate.ToString("yyyy-MM-dd"), x.InvoiceCount, x.TotalSales, x.CashSales, x.MobileSales, x.CardSales, x.DueSales, x.DueCollected, x.PurchasePaid, x.TotalProfit }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task CloseAsync()
    {
        try
        {
            var ok = MessageBox.Show($"Close {dtDate.Value:yyyy-MM-dd}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;
            _last = await ApiClient.Instance.PostAsync<object, ClosingDto>("api/bangladesh/closings",
                new { businessDate = dtDate.Value.ToString("yyyy-MM-dd"), branchId = BdSettings.Current.CurrentBranchId, notes = (string?)null });
            if (_last is null) return;
            lblSummary.Text = $"Date: {_last.BusinessDate:yyyy-MM-dd}\nInvoices: {_last.InvoiceCount}  Sales: {_last.TotalSales:0.00}\n" +
                $"Cash: {_last.CashSales:0.00}  Mobile: {_last.MobileSales:0.00}  Card: {_last.CardSales:0.00}\n" +
                $"Due sale: {_last.DueSales:0.00}  Due collected: {_last.DueCollected:0.00}\n" +
                $"Supplier paid: {_last.PurchasePaid:0.00}  Profit: {_last.TotalProfit:0.00}";
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Close failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportCsv()
    {
        if (_last is null) { MessageBox.Show("Close a day first (or select after refresh)."); return; }
        using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"closing-{_last.BusinessDate:yyyy-MM-dd}.csv" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var sb = new StringBuilder("Field,Amount\n");
        sb.AppendLine($"Date,{_last.BusinessDate:yyyy-MM-dd}").AppendLine($"Invoices,{_last.InvoiceCount}")
            .AppendLine($"TotalSales,{_last.TotalSales:0.00}").AppendLine($"Cash,{_last.CashSales:0.00}")
            .AppendLine($"Mobile,{_last.MobileSales:0.00}").AppendLine($"Card,{_last.CardSales:0.00}")
            .AppendLine($"DueSales,{_last.DueSales:0.00}").AppendLine($"DueCollected,{_last.DueCollected:0.00}")
            .AppendLine($"SupplierPaid,{_last.PurchasePaid:0.00}").AppendLine($"Profit,{_last.TotalProfit:0.00}");
        File.WriteAllText(dlg.FileName, sb.ToString());
        MessageBox.Show("CSV saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ── Settings: shop profile, Single/Multi, language, theme, backup ──
public class SettingsForm : Form
{
    private readonly TextBox txtName = new() { Width = 260 };
    private readonly TextBox txtAddress = new() { Width = 260 };
    private readonly TextBox txtPhone = new() { Width = 160, PlaceholderText = "01XXXXXXXXX" };
    private readonly TextBox txtDgda = new() { Width = 160, PlaceholderText = "DGDA License" };
    private readonly TextBox txtTrade = new() { Width = 160, PlaceholderText = "Trade License" };
    private readonly ComboBox cmbMode = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cmbBranch = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cmbLang = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cmbTheme = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox chkBig = new() { Text = "Big Font", AutoSize = true };
    private List<BranchDto> _branches = [];

    public SettingsForm()
    {
        Text = Lang.T("settings");
        var s = BdSettings.Current;
        txtName.Text = s.PharmacyName; txtAddress.Text = s.Address ?? ""; txtPhone.Text = s.Phone ?? "";
        txtDgda.Text = s.DgdaLicense ?? ""; txtTrade.Text = s.TradeLicense ?? "";
        cmbMode.Items.AddRange(["Single", "Multi"]); cmbMode.SelectedItem = s.Mode;
        cmbLang.Items.AddRange(["English", "বাংলা"]); cmbLang.SelectedIndex = s.Language == "bn" ? 1 : 0;
        cmbTheme.Items.AddRange(["Light", "Dark"]); cmbTheme.SelectedItem = s.Theme;
        chkBig.Checked = s.BigFont;

        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(16), AutoScroll = true };
        var btnSave = new Button { Text = Lang.T("save"), Width = 120, Height = 36, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        var btnBackup = new Button { Text = "Backup JSON", Width = 120, Height = 36 };
        btnSave.Click += (_, _) => Save();
        btnBackup.Click += async (_, _) => await BackupAsync();
        p.Controls.AddRange([
            new Label { Text = "Pharmacy Name", AutoSize = true }, txtName,
            new Label { Text = "Address", AutoSize = true }, txtAddress,
            new Label { Text = "Phone", AutoSize = true }, txtPhone,
            new Label { Text = "DGDA License", AutoSize = true }, txtDgda,
            new Label { Text = "Trade License", AutoSize = true }, txtTrade,
            new Label { Text = "Shop Mode (Single/Multi)", AutoSize = true }, cmbMode,
            new Label { Text = "Current Branch", AutoSize = true }, cmbBranch,
            new Label { Text = "Language", AutoSize = true }, cmbLang,
            new Label { Text = "Theme", AutoSize = true }, cmbTheme, chkBig, btnSave, btnBackup,
            new Label { Text = "Backup: JSON snapshot download hoy. Full .bak SSMS diye nin.", AutoSize = true }]);
        Controls.Add(p);
        Shown += async (_, _) => { Theme.Apply(this); await LoadBranchesAsync(); };
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            _branches = await ApiClient.Instance.GetAsync<List<BranchDto>>("api/bangladesh/branches") ?? [];
            cmbBranch.Items.Clear();
            foreach (var b in _branches) cmbBranch.Items.Add($"{b.BranchId} - {b.BranchName}");
            var cur = BdSettings.Current.CurrentBranchId;
            var idx = _branches.FindIndex(b => b.BranchId == cur);
            cmbBranch.SelectedIndex = idx >= 0 ? idx : (_branches.Count > 0 ? 0 : -1);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Branches failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Save()
    {
        var s = BdSettings.Current;
        s.PharmacyName = txtName.Text.Trim();
        s.Address = txtAddress.Text.Trim(); s.Phone = txtPhone.Text.Trim();
        s.DgdaLicense = txtDgda.Text.Trim(); s.TradeLicense = txtTrade.Text.Trim();
        s.Mode = cmbMode.SelectedItem?.ToString() ?? "Single";
        if (cmbBranch.SelectedIndex >= 0 && cmbBranch.SelectedIndex < _branches.Count)
        {
            s.CurrentBranchId = s.Mode == "Multi" ? _branches[cmbBranch.SelectedIndex].BranchId : null;
            s.CurrentBranchName = _branches[cmbBranch.SelectedIndex].BranchName;
        }
        else { s.CurrentBranchId = null; s.CurrentBranchName = null; }
        s.Language = cmbLang.SelectedIndex == 1 ? "bn" : "en";
        s.Theme = cmbTheme.SelectedItem?.ToString() ?? "Light";
        s.BigFont = chkBig.Checked;
        BdSettings.Save();
        Theme.Apply(this);
        MessageBox.Show("Saved. Restart module (Dashboard/POS reopen) to apply language.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task BackupAsync()
    {
        try
        {
            var snap = await ApiClient.Instance.GetAsync<JsonElement>("api/bangladesh/backup/snapshot");
            using var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = $"backup-{DateTime.Now:yyyyMMdd-HHmm}.json" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true }));
            MessageBox.Show("Backup saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Backup failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
