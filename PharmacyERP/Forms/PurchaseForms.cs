using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

public class SuppliersForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtName = new() { Width = 200, PlaceholderText = "Supplier name" };
    private readonly TextBox txtPhone = new() { Width = 130, PlaceholderText = "Phone" };

    public SuppliersForm()
    {
        Text = "Suppliers";
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        var btnAdd = new Button { Text = "Add", Width = 80 };
        var btnDel = new Button { Text = "Delete", Width = 80 };
        var btnRefresh = new Button { Text = "Refresh", Width = 80 };
        btnAdd.Click += async (_, _) => await AddAsync();
        btnDel.Click += async (_, _) => await DeleteAsync();
        btnRefresh.Click += async (_, _) => await LoadAsync();
        top.Controls.AddRange([txtName, txtPhone, btnAdd, btnDel, btnRefresh]);
        Controls.Add(grid); Controls.Add(top);
        Shown += async (_, _) => await LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            var items = await ApiClient.Instance.GetAsync<List<SupplierDto>>("api/suppliers") ?? [];
            grid.DataSource = items.Select(s => new { s.SupplierId, s.SupplierName, s.Phone, s.TotalPurchase, s.OutstandingBalance }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Name required."); return; }
        try
        {
            await ApiClient.Instance.PostAsync<object, object>("api/suppliers", new { supplierName = txtName.Text.Trim(), phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim() });
            txtName.Clear(); txtPhone.Clear(); await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Add failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task DeleteAsync()
    {
        if (grid.CurrentRow is null) return;
        var id = Convert.ToInt32(grid.CurrentRow.Cells["SupplierId"].Value);
        try { await ApiClient.Instance.DeleteAsync($"api/suppliers/{id}"); await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Delete failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

public class PurchaseForm : Form
{
    private readonly ComboBox cmbSupplier = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtMedId = new() { Width = 80, PlaceholderText = "Med ID" };
    private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 100000, Value = 10, Width = 80 };
    private readonly NumericUpDown numPrice = new() { Maximum = 1000000, DecimalPlaces = 2, Width = 100 };
    private readonly TextBox txtBatch = new() { Width = 100, PlaceholderText = "Batch" };
    private readonly NumericUpDown numPaid = new() { Maximum = 100000000, DecimalPlaces = 2, Width = 120 };
    private readonly List<PurchaseLine> lines = [];
    private record PurchaseLine(int MedicineId, int Qty, decimal Price, string? Batch);

    public PurchaseForm()
    {
        Text = "Purchase / Stock Receive";
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(6) };
        var btnAddLine = new Button { Text = "Add Line", Width = 90 };
        var btnReceive = new Button { Text = "Receive Stock", Width = 120, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        btnAddLine.Click += (_, _) =>
        {
            if (!int.TryParse(txtMedId.Text, out var mid)) { MessageBox.Show("Med ID numeric."); return; }
            lines.Add(new PurchaseLine(mid, (int)numQty.Value, numPrice.Value, string.IsNullOrWhiteSpace(txtBatch.Text) ? null : txtBatch.Text.Trim()));
            grid.DataSource = lines.Select(l => new { l.MedicineId, l.Qty, l.Price, Total = l.Qty * l.Price, l.Batch }).ToList();
        };
        btnReceive.Click += async (_, _) => await ReceiveAsync();
        top.Controls.AddRange([new Label { Text = "Supplier", AutoSize = true }, cmbSupplier, new Label { Text = "Med", AutoSize = true }, txtMedId, new Label { Text = "Qty", AutoSize = true }, numQty, new Label { Text = "Price", AutoSize = true }, numPrice, txtBatch, btnAddLine, new Label { Text = "Paid", AutoSize = true }, numPaid, btnReceive]);
        Controls.Add(grid); Controls.Add(top);
        Shown += async (_, _) => await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        var items = await ApiClient.Instance.GetAsync<List<SupplierDto>>("api/suppliers") ?? [];
        cmbSupplier.DataSource = items; cmbSupplier.DisplayMember = "SupplierName"; cmbSupplier.ValueMember = "SupplierId";
    }

    private async Task ReceiveAsync()
    {
        if (cmbSupplier.SelectedValue is not int sid && !int.TryParse(cmbSupplier.SelectedValue?.ToString(), out sid)) { MessageBox.Show("Select supplier."); return; }
        if (lines.Count == 0) { MessageBox.Show("Add lines first."); return; }
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, ReceiveRes>("api/purchases", new
            {
                supplierId = Convert.ToInt32(cmbSupplier.SelectedValue),
                items = lines.Select(l => new { medicineId = l.MedicineId, quantity = l.Qty, unitPrice = l.Price, batchNumber = l.Batch }).ToList(),
                paidAmount = numPaid.Value
            });
            MessageBox.Show($"Received!\nPO: {res?.PurchaseNumber}\nDue: {res?.DueAmount:0.00} ({res?.PaymentStatus})", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lines.Clear(); grid.DataSource = null;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Receive failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private record ReceiveRes(string PurchaseNumber, decimal DueAmount, string PaymentStatus);
}
