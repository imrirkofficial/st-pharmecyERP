using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

public class PosForm : Form
{
    private readonly DataGridView gridSearch = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridCart = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtSearch = new() { Width = 200, PlaceholderText = "Medicine / barcode..." };
    private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 10000, Value = 1, Width = 70 };
    private readonly TextBox txtCustomerId = new() { Width = 80, PlaceholderText = "Cust ID" };
    private readonly NumericUpDown numDiscount = new() { Maximum = 1000000, DecimalPlaces = 2, Width = 100 };
    private readonly NumericUpDown numPaid = new() { Maximum = 10000000, DecimalPlaces = 2, Width = 120 };
    private readonly ComboBox cmbPay = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
    private readonly List<CartLine> cart = [];

    private record CartLine(int MedicineId, string Name, decimal Price, int Qty) { public decimal Total => Price * Qty; }

    public PosForm()
    {
        Text = "Sales / POS";
        cmbPay.Items.AddRange(["Cash", "Card", "Mobile Banking"]);
        cmbPay.SelectedIndex = 0;

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 480 };
        var leftTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
        var btnFind = new Button { Text = "Find", Width = 70 };
        var btnAdd = new Button { Text = "Add to Cart >", Width = 100 };
        btnFind.Click += async (_, _) => await SearchAsync();
        btnAdd.Click += (_, _) => AddToCart();
        leftTop.Controls.AddRange([txtSearch, btnFind, new Label { Text = "Qty", AutoSize = true }, numQty, btnAdd]);
        var left = new Panel { Dock = DockStyle.Fill };
        left.Controls.Add(gridSearch); left.Controls.Add(leftTop);

        var rightTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(6) };
        var btnCheckout = new Button { Text = "Checkout (F5)", Width = 120, Height = 40, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        btnCheckout.Click += async (_, _) => await CheckoutAsync();
        rightTop.Controls.AddRange([new Label { Text = "Cust ID", AutoSize = true }, txtCustomerId, new Label { Text = "Discount", AutoSize = true }, numDiscount, new Label { Text = "Paid", AutoSize = true }, numPaid, new Label { Text = "Pay", AutoSize = true }, cmbPay, btnCheckout, lblTotal]);
        var right = new Panel { Dock = DockStyle.Fill };
        right.Controls.Add(gridCart); right.Controls.Add(rightTop);

        split.Panel1.Controls.Add(left); split.Panel2.Controls.Add(right);
        Controls.Add(split);
        Shown += async (_, _) => await SearchAsync();
    }

    private async Task SearchAsync()
    {
        try
        {
            var url = "api/medicines?pageSize=100";
            if (!string.IsNullOrWhiteSpace(txtSearch.Text)) url += "&search=" + Uri.EscapeDataString(txtSearch.Text.Trim());
            var items = await ApiClient.Instance.GetAsync<List<MedicineDto>>(url) ?? [];
            gridSearch.DataSource = items.Select(m => new { m.MedicineId, m.MedicineName, m.SalePrice, m.StockQuantity }).ToList();
            gridSearch.Tag = items;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Search failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AddToCart()
    {
        if (gridSearch.CurrentRow is null) return;
        var items = (gridSearch.Tag as List<MedicineDto>) ?? [];
        var id = Convert.ToInt32(gridSearch.CurrentRow.Cells["MedicineId"].Value);
        var m = items.FirstOrDefault(x => x.MedicineId == id);
        if (m is null) return;
        var qty = (int)numQty.Value;
        if (m.StockQuantity < qty) { MessageBox.Show($"Stock only {m.StockQuantity}.", "Insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var line = cart.FirstOrDefault(c => c.MedicineId == id);
        if (line is not null) cart.Remove(line);
        cart.Add(new CartLine(id, m.MedicineName, m.SalePrice, (line?.Qty ?? 0) + qty));
        RefreshCart();
    }

    private void RefreshCart()
    {
        gridCart.DataSource = cart.Select(c => new { c.MedicineId, c.Name, c.Price, c.Qty, c.Total }).ToList();
        var total = cart.Sum(c => c.Total);
        lblTotal.Text = $"Total: {total:0.00}  Net: {total - numDiscount.Value:0.00}";
    }

    private async Task CheckoutAsync()
    {
        if (cart.Count == 0) { MessageBox.Show("Cart empty."); return; }
        var total = cart.Sum(c => c.Total);
        var net = total - numDiscount.Value;
        if (numPaid.Value < net) { MessageBox.Show($"Paid ({numPaid.Value}) < net ({net})."); return; }
        try
        {
            int? cid = int.TryParse(txtCustomerId.Text, out var c) ? c : null;
            var res = await ApiClient.Instance.PostAsync<object, CheckoutRes>("api/sales", new
            {
                customerId = cid,
                items = cart.Select(c => new { medicineId = c.MedicineId, quantity = c.Qty }).ToList(),
                discountAmount = numDiscount.Value,
                paidAmount = numPaid.Value,
                paymentMethod = cmbPay.SelectedItem?.ToString() ?? "Cash"
            });
            MessageBox.Show($"Sale OK!\nInvoice: {res?.InvoiceNumber}\nNet: {res?.NetAmount:0.00}  Change: {res?.ChangeAmount:0.00}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            cart.Clear(); RefreshCart(); await SearchAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Checkout failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private record CheckoutRes(string InvoiceNumber, decimal NetAmount, decimal ChangeAmount);
}
