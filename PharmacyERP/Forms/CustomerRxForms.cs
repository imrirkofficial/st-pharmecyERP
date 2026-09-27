using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

public class CustomersForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtName = new() { Width = 200, PlaceholderText = "Customer name" };
    private readonly TextBox txtPhone = new() { Width = 130, PlaceholderText = "Phone" };

    public CustomersForm()
    {
        Text = "Customers";
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        var btnAdd = new Button { Text = "Add", Width = 80 };
        var btnRefresh = new Button { Text = "Refresh", Width = 80 };
        btnAdd.Click += async (_, _) => await AddAsync();
        btnRefresh.Click += async (_, _) => await LoadAsync();
        top.Controls.AddRange([txtName, txtPhone, btnAdd, btnRefresh]);
        Controls.Add(grid); Controls.Add(top);
        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var items = await ApiClient.Instance.GetAsync<List<CustomerDto>>("api/customers") ?? [];
            grid.DataSource = items.Select(c => new { c.CustomerId, c.CustomerName, c.Phone, c.TotalPurchase }).ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Name required."); return; }
        try
        {
            await ApiClient.Instance.PostAsync<object, object>("api/customers", new { customerName = txtName.Text.Trim(), phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim() });
            txtName.Clear(); txtPhone.Clear(); await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Add failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

public class PrescriptionsForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtCustId = new() { Width = 70, PlaceholderText = "CustID" };
    private readonly TextBox txtDoctor = new() { Width = 130, PlaceholderText = "Doctor" };
    private readonly TextBox txtMedId = new() { Width = 70, PlaceholderText = "MedID" };
    private readonly TextBox txtDosage = new() { Width = 90, PlaceholderText = "1+0+1" };
    private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 10000, Value = 1, Width = 60 };
    private readonly NumericUpDown numPaid = new() { Maximum = 10000000, DecimalPlaces = 2, Width = 100 };
    private readonly List<object> lines = [];

    public PrescriptionsForm()
    {
        Text = "Prescriptions";
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(6) };
        var btnAddLine = new Button { Text = "Add Line", Width = 80 };
        var btnCreate = new Button { Text = "Create RX", Width = 90 };
        var btnFulfill = new Button { Text = "Fulfill Selected", Width = 110, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        var btnRefresh = new Button { Text = "Refresh", Width = 80 };
        btnAddLine.Click += (_, _) =>
        {
            if (!int.TryParse(txtMedId.Text, out var mid)) { MessageBox.Show("Med ID numeric."); return; }
            lines.Add(new { medicineId = mid, dosage = string.IsNullOrWhiteSpace(txtDosage.Text) ? "1+0+1" : txtDosage.Text, quantity = (int)numQty.Value, duration = 5 });
            MessageBox.Show($"Line added ({lines.Count}).", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        btnCreate.Click += async (_, _) => await CreateAsync();
        btnFulfill.Click += async (_, _) => await FulfillAsync();
        btnRefresh.Click += async (_, _) => await LoadAsync();
        top.Controls.AddRange([new Label { Text = "Cust", AutoSize = true }, txtCustId, new Label { Text = "Dr", AutoSize = true }, txtDoctor, new Label { Text = "Med", AutoSize = true }, txtMedId, txtDosage, numQty, btnAddLine, btnCreate, new Label { Text = "Paid", AutoSize = true }, numPaid, btnFulfill, btnRefresh]);
        Controls.Add(grid); Controls.Add(top);
        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var res = await ApiClient.Instance.GetAsync<RxList>("api/prescriptions?pageSize=100") ?? new RxList(0, 1, 20, []);
            grid.DataSource = res.Items.Select(i => new { i.PrescriptionId, i.PrescriptionNumber, i.CustomerName, i.DoctorName, Fulfilled = i.IsFulfilled ? "YES" : "" }).ToList();
            grid.Tag = res.Items;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task CreateAsync()
    {
        if (!int.TryParse(txtCustId.Text, out var cid)) { MessageBox.Show("Customer ID numeric."); return; }
        if (lines.Count == 0) { MessageBox.Show("Add lines first."); return; }
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, RxCreated>("api/prescriptions", new { customerId = cid, doctorName = string.IsNullOrWhiteSpace(txtDoctor.Text) ? null : txtDoctor.Text.Trim(), items = lines });
            MessageBox.Show($"RX created: {res?.PrescriptionNumber}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lines.Clear(); await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Create failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task FulfillAsync()
    {
        if (grid.CurrentRow is null) return;
        var id = Convert.ToInt32(grid.CurrentRow.Cells["PrescriptionId"].Value);
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, FulfillRes>($"api/prescriptions/{id}/fulfill", new { discountAmount = 0, paidAmount = numPaid.Value, paymentMethod = "Cash" });
            MessageBox.Show($"Fulfilled!\nInvoice: {res?.InvoiceNumber}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message + "\nHint: Paid must cover total. Increase Paid.", "Fulfill failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private record RxItem(int PrescriptionId, string PrescriptionNumber, string? CustomerName, string? DoctorName, bool IsFulfilled);
    private record RxList(int Total, int Page, int PageSize, List<RxItem> Items);
    private record RxCreated(string PrescriptionNumber);
    private record FulfillRes(string InvoiceNumber);
}
