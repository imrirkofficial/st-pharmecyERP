using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

public class MedicineEditDialog : Form
{
    private readonly TextBox txtName = new() { Width = 250 };
    private readonly TextBox txtGeneric = new() { Width = 250 };
    private readonly TextBox txtMfr = new() { Width = 250 };
    private readonly TextBox txtCategory = new() { Width = 250 };
    private readonly NumericUpDown numBuy = new() { Maximum = 1000000, DecimalPlaces = 2, Width = 120 };
    private readonly NumericUpDown numSale = new() { Maximum = 1000000, DecimalPlaces = 2, Width = 120 };
    private readonly NumericUpDown numStock = new() { Maximum = 1000000, Width = 120 };
    private readonly NumericUpDown numReorder = new() { Maximum = 1000000, Width = 120, Value = 10 };
    private readonly TextBox txtBarcode = new() { Width = 250 };
    private readonly TextBox txtShelf = new() { Width = 250 };
    private readonly DateTimePicker dtExpiry = new() { ShowCheckBox = true, Checked = false, Width = 250 };

    public string MName => txtName.Text.Trim();
    public string? MGeneric => string.IsNullOrWhiteSpace(txtGeneric.Text) ? null : txtGeneric.Text.Trim();
    public string? MMfr => string.IsNullOrWhiteSpace(txtMfr.Text) ? null : txtMfr.Text.Trim();
    public string? MCategory => string.IsNullOrWhiteSpace(txtCategory.Text) ? null : txtCategory.Text.Trim();
    public decimal MBuy => numBuy.Value;
    public decimal MSale => numSale.Value;
    public int MStock => (int)numStock.Value;
    public int MReorder => (int)numReorder.Value;
    public string? MBarcode => string.IsNullOrWhiteSpace(txtBarcode.Text) ? null : txtBarcode.Text.Trim();
    public string? MShelf => string.IsNullOrWhiteSpace(txtShelf.Text) ? null : txtShelf.Text.Trim();
    public DateTime? MExpiry => dtExpiry.Checked ? dtExpiry.Value.Date : null;

    public MedicineEditDialog(MedicineDto? existing = null)
    {
        Text = existing is null ? "Add Medicine" : "Edit Medicine";
        Size = new Size(380, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var p = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 11, ColumnCount = 2, Padding = new Padding(12), AutoScroll = true };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Row(string label, Control c) { p.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }); p.Controls.Add(c); }
        Row("Name*", txtName); Row("Generic", txtGeneric); Row("Manufacturer", txtMfr); Row("Category", txtCategory);
        Row("Buy Price", numBuy); Row("Sale Price", numSale); Row("Stock", numStock); Row("Reorder", numReorder);
        Row("Barcode", txtBarcode); Row("Shelf", txtShelf); Row("Expiry", dtExpiry);

        var btns = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 45, Padding = new Padding(8) };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        btns.Controls.Add(ok); btns.Controls.Add(cancel);
        Controls.Add(p); Controls.Add(btns);
        AcceptButton = ok; CancelButton = cancel;

        if (existing is not null)
        {
            txtName.Text = existing.MedicineName; txtGeneric.Text = existing.GenericName ?? "";
            txtMfr.Text = existing.Manufacturer ?? ""; txtCategory.Text = existing.Category ?? "";
            numBuy.Value = existing.PurchasePrice; numSale.Value = existing.SalePrice;
            numStock.Value = existing.StockQuantity; numReorder.Value = existing.ReorderLevel;
            txtBarcode.Text = existing.Barcode ?? ""; txtShelf.Text = existing.ShelfLocation ?? "";
            if (existing.ExpiryDate.HasValue) { dtExpiry.Value = existing.ExpiryDate.Value; dtExpiry.Checked = true; }
        }
    }
}

public class MedicinesForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtSearch = new() { Width = 220, PlaceholderText = "Search name / barcode..." };
    private readonly CheckBox chkLow = new() { Text = "Low stock only", AutoSize = true };
    private readonly CheckBox chkExp = new() { Text = "Expiring (30d)", AutoSize = true };

    public MedicinesForm()
    {
        Text = "Medicines";
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        var btnSearch = new Button { Text = "Search", Width = 80 };
        var btnAdd = new Button { Text = "Add", Width = 80 };
        var btnEdit = new Button { Text = "Edit", Width = 80 };
        var btnDel = new Button { Text = "Delete", Width = 80 };
        var btnRefresh = new Button { Text = "Refresh", Width = 80 };
        btnSearch.Click += async (_, _) => await LoadAsync();
        btnRefresh.Click += async (_, _) => await LoadAsync();
        btnAdd.Click += async (_, _) => await AddAsync();
        btnEdit.Click += async (_, _) => await EditAsync();
        btnDel.Click += async (_, _) => await DeleteAsync();
        top.Controls.AddRange([txtSearch, btnSearch, chkLow, chkExp, btnAdd, btnEdit, btnDel, btnRefresh]);
        Controls.Add(grid); Controls.Add(top);
        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var url = "api/medicines?pageSize=200";
            if (!string.IsNullOrWhiteSpace(txtSearch.Text)) url += "&search=" + Uri.EscapeDataString(txtSearch.Text.Trim());
            if (chkLow.Checked) url += "&lowStock=true";
            if (chkExp.Checked) url += "&expiring=true";
            var items = await ApiClient.Instance.GetAsync<List<MedicineDto>>(url) ?? [];
            grid.DataSource = items.Select(m => new { m.MedicineId, m.MedicineName, m.GenericName, m.Category, m.SalePrice, m.StockQuantity, m.ReorderLevel, Low = m.IsLowStock ? "YES" : "", Exp = m.IsExpiringSoon ? "YES" : "", m.Barcode, m.ShelfLocation }).ToList();
            grid.Tag = items;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private List<MedicineDto> Items() => (grid.Tag as List<MedicineDto>) ?? [];
    private MedicineDto? Selected()
    {
        if (grid.CurrentRow is null) return null;
        var id = Convert.ToInt32(grid.CurrentRow.Cells["MedicineId"].Value);
        return Items().FirstOrDefault(m => m.MedicineId == id);
    }

    private async Task AddAsync()
    {
        using var d = new MedicineEditDialog();
        if (d.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(d.MName)) return;
        try
        {
            await ApiClient.Instance.PostAsync<object, object>("api/medicines", new
            {
                medicineName = d.MName, genericName = d.MGeneric, manufacturer = d.MMfr, category = d.MCategory,
                purchasePrice = d.MBuy, salePrice = d.MSale, stockQuantity = d.MStock, reorderLevel = d.MReorder,
                barcode = d.MBarcode, shelfLocation = d.MShelf, expiryDate = d.MExpiry
            });
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Add failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task EditAsync()
    {
        var m = Selected();
        if (m is null) { MessageBox.Show("Select a row first."); return; }
        using var d = new MedicineEditDialog(m);
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await ApiClient.Instance.PutAsync<object, object>($"api/medicines/{m.MedicineId}", new
            {
                medicineName = d.MName, genericName = d.MGeneric, manufacturer = d.MMfr, category = d.MCategory,
                purchasePrice = d.MBuy, salePrice = d.MSale, stockQuantity = d.MStock, reorderLevel = d.MReorder,
                barcode = d.MBarcode, shelfLocation = d.MShelf, expiryDate = d.MExpiry, isActive = true
            });
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task DeleteAsync()
    {
        var m = Selected();
        if (m is null) return;
        if (MessageBox.Show($"Delete '{m.MedicineName}'? (soft delete)", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { await ApiClient.Instance.DeleteAsync($"api/medicines/{m.MedicineId}"); await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Delete failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
