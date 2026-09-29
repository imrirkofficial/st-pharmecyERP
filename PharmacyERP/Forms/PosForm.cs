using PharmacyERP.Models;
using PharmacyERP.Services;

namespace PharmacyERP.Forms;

/// <summary>Keyboard-first POS: F1 search, F2 add, F3 discount, F5 checkout, Esc clear.
/// Pcs/Pata/Box unit, bKash/Nagad/Rocket split, Due (baki), VAT, thermal receipt preview.</summary>
public class PosForm : Form
{
    private readonly DataGridView gridSearch = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView gridCart = new() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtSearch = new() { Width = 190, PlaceholderText = "Medicine / barcode (F1)" };
    private readonly NumericUpDown numQty = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 70 };
    private readonly ComboBox cmbUnit = new() { Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtCustomerId = new() { Width = 70, PlaceholderText = "Cust ID" };
    private readonly NumericUpDown numDiscount = new() { Maximum = 1000000, DecimalPlaces = 2, Width = 90 };
    private readonly NumericUpDown numVat = new() { Maximum = 1000000, DecimalPlaces = 2, Width = 80 };
    private readonly NumericUpDown numPaid = new() { Maximum = 10000000, DecimalPlaces = 2, Width = 110 };
    private readonly ComboBox cmbPay = new() { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtTrx = new() { Width = 110, PlaceholderText = "TrxID" };
    private readonly Label lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
    private readonly List<CartLine> cart = [];

    private record CartLine(int MedicineId, string Name, decimal PcsPrice, int QtyPcs, string UnitLabel, bool Controlled)
    { public decimal Total => PcsPrice * QtyPcs; }

    public PosForm()
    {
        Text = $"{Lang.T("salesPos")}  [F1 Search | F2 Add | F3 Disc | F5 Bill | Esc Clear]";
        KeyPreview = true;
        KeyDown += OnKey;
        cmbPay.Items.AddRange(["Cash", "bKash", "Nagad", "Rocket", "Card", "Due"]);
        cmbPay.SelectedIndex = 0;
        cmbPay.SelectedIndexChanged += (_, _) => txtTrx.Enabled = IsMobile();
        cmbUnit.Items.AddRange(["Pcs", "Pata", "Box"]);
        cmbUnit.SelectedIndex = 0;

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 470 };
        var leftTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        var btnFind = new Button { Text = $"{Lang.T("search")} (F1)", Width = 100 };
        var btnAdd = new Button { Text = "Add > (F2)", Width = 100 };
        btnFind.Click += async (_, _) => await SearchAsync();
        btnAdd.Click += (_, _) => AddToCart();
        txtSearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };
        leftTop.Controls.AddRange([txtSearch, btnFind, new Label { Text = "Qty", AutoSize = true }, numQty, cmbUnit, btnAdd]);
        var left = new Panel { Dock = DockStyle.Fill };
        left.Controls.Add(gridSearch); left.Controls.Add(leftTop);

        var rightTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(6) };
        var btnCheckout = new Button { Text = $"{Lang.T("checkout")} (F5)", Width = 130, Height = 44, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
        var btnClear = new Button { Text = "Clear (Esc)", Width = 100, Height = 44 };
        btnCheckout.Click += async (_, _) => await CheckoutAsync();
        btnClear.Click += (_, _) => ClearAll();
        numDiscount.ValueChanged += (_, _) => RefreshCart();
        numVat.ValueChanged += (_, _) => RefreshCart();
        rightTop.Controls.AddRange([new Label { Text = "Cust ID", AutoSize = true }, txtCustomerId,
            new Label { Text = Lang.T("discount"), AutoSize = true }, numDiscount,
            new Label { Text = "VAT", AutoSize = true }, numVat,
            new Label { Text = Lang.T("paid"), AutoSize = true }, numPaid,
            new Label { Text = "Pay", AutoSize = true }, cmbPay,
            new Label { Text = "TrxID", AutoSize = true }, txtTrx, btnCheckout, btnClear, lblTotal]);
        var right = new Panel { Dock = DockStyle.Fill };
        right.Controls.Add(gridCart); right.Controls.Add(rightTop);

        split.Panel1.Controls.Add(left); split.Panel2.Controls.Add(right);
        Controls.Add(split);
        Shown += async (_, _) => { Theme.Apply(this); await SearchAsync(); };
    }

    private bool IsMobile() => cmbPay.SelectedItem?.ToString() is "bKash" or "Nagad" or "Rocket";

    private void OnKey(object? s, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F1: txtSearch.Focus(); txtSearch.SelectAll(); e.Handled = true; break;
            case Keys.F2: AddToCart(); e.Handled = true; break;
            case Keys.F3: numDiscount.Focus(); e.Handled = true; break;
            case Keys.F5: _ = CheckoutAsync(); e.Handled = true; break;
            case Keys.Escape: ClearAll(); e.Handled = true; break;
        }
    }

    private void ClearAll()
    {
        cart.Clear(); RefreshCart();
        numDiscount.Value = 0; numVat.Value = 0; numPaid.Value = 0;
        txtCustomerId.Clear(); txtTrx.Clear(); txtSearch.Clear();
        txtSearch.Focus();
    }

    private async Task SearchAsync()
    {
        try
        {
            var q = txtSearch.Text.Trim();
            // barcode exact match first
            if (!string.IsNullOrWhiteSpace(q))
            {
                try
                {
                    var one = await ApiClient.Instance.GetAsync<MedicineDto>("api/bangladesh/barcode/" + Uri.EscapeDataString(q));
                    if (one is not null)
                    {
                        gridSearch.DataSource = new[] { Row(one) };
                        gridSearch.Tag = new List<MedicineDto> { one };
                        return;
                    }
                }
                catch { /* fall through to text search */ }
            }
            var url = "api/medicines?pageSize=100";
            if (!string.IsNullOrWhiteSpace(q)) url += "&search=" + Uri.EscapeDataString(q);
            var items = await ApiClient.Instance.GetAsync<List<MedicineDto>>(url) ?? [];
            gridSearch.DataSource = items.Select(Row).ToList();
            gridSearch.Tag = items;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Search failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static object Row(MedicineDto m) => new
    {
        m.MedicineId, m.MedicineName, m.SalePrice, m.StockQuantity,
        Unit = $"{m.UnitsPerStrip}/pata", CTL = m.IsControlled ? "!" : ""
    };

    private void AddToCart()
    {
        if (gridSearch.CurrentRow is null) return;
        var items = (gridSearch.Tag as List<MedicineDto>) ?? [];
        var id = Convert.ToInt32(gridSearch.CurrentRow.Cells["MedicineId"].Value);
        var m = items.FirstOrDefault(x => x.MedicineId == id);
        if (m is null) return;
        var ups = Math.Max(1, m.UnitsPerStrip);
        var per = cmbUnit.SelectedItem?.ToString() switch
        {
            "Pata" => ups,
            "Box" => ups * Math.Max(1, m.StripsPerBox),
            _ => 1
        };
        var qtyPcs = (int)numQty.Value * per;
        if (m.StockQuantity < qtyPcs) { MessageBox.Show($"Stock only {m.StockQuantity} pcs.", "Insufficient", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (m.IsControlled)
            MessageBox.Show($"{m.MedicineName}\n{Lang.T("controlledWarn")}", "!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        var line = cart.FirstOrDefault(c => c.MedicineId == id);
        if (line is not null) cart.Remove(line);
        cart.Add(new CartLine(id, m.MedicineName, m.SalePrice, (line?.QtyPcs ?? 0) + qtyPcs,
            $"{numQty.Value} {cmbUnit.SelectedItem}", m.IsControlled));
        RefreshCart();
    }

    private void RefreshCart()
    {
        gridCart.DataSource = cart.Select(c => new { c.MedicineId, c.Name, c.UnitLabel, Pcs = c.QtyPcs, c.PcsPrice, c.Total }).ToList();
        var total = cart.Sum(c => c.Total);
        var net = total - numDiscount.Value + numVat.Value;
        lblTotal.Text = $"{Lang.T("total")}: {total:0.00}  {Lang.T("net")}: {net:0.00}";
    }

    private async Task CheckoutAsync()
    {
        if (cart.Count == 0) { MessageBox.Show(Lang.T("cartEmpty")); return; }
        var total = cart.Sum(c => c.Total);
        var net = total - numDiscount.Value + numVat.Value;
        if (net < 0) { MessageBox.Show("Discount exceeds total."); return; }
        var method = cmbPay.SelectedItem?.ToString() ?? "Cash";
        var paid = method == "Due" ? 0 : numPaid.Value;
        // Cash/Card/Mobile: Paid=0 means full payment (user didn't type). Otherwise every sale becomes Due and fails.
        if (method != "Due" && paid == 0)
        {
            paid = net;
            numPaid.Value = paid;
        }
        int? cid = int.TryParse(txtCustomerId.Text, out var c) ? c : null;
        if (net - paid > 0 && cid is null) { MessageBox.Show("Due sale needs Customer ID (Baki Khata)."); return; }
        if (IsMobile() && string.IsNullOrWhiteSpace(txtTrx.Text))
        {
            var ok = MessageBox.Show("TrxID empty. Continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;
        }
        try
        {
            var res = await ApiClient.Instance.PostAsync<object, SaleCheckoutDto>("api/sales", new
            {
                customerId = cid,
                items = cart.Select(x => new { medicineId = x.MedicineId, quantity = x.QtyPcs }).ToList(),
                discountAmount = numDiscount.Value,
                paidAmount = paid,
                paymentMethod = method == "Due" ? "Due" : method,
                trxId = string.IsNullOrWhiteSpace(txtTrx.Text) ? null : txtTrx.Text.Trim(),
                branchId = BdSettings.Current.CurrentBranchId,
                vatAmount = numVat.Value,
                payments = method == "Due" ? null
                    : new[] { new { method, amount = Math.Min(paid, net), trxId = string.IsNullOrWhiteSpace(txtTrx.Text) ? null : txtTrx.Text.Trim() } },
                notes = (string?)null
            });
            if (res is null) return;
            var pays = new List<Receipt.ReceiptPay> { new(method, Math.Min(paid, net), txtTrx.Text.Trim()) };
            var text = Receipt.Build(res.InvoiceNumber, res.SaleDate,
                cart.Select(x => new Receipt.ReceiptItem(x.Name, x.QtyPcs, x.PcsPrice)).ToList(),
                total, numDiscount.Value, net, pays, res.DueAmount, res.ChangeAmount, res.CustomerName);
            MessageBox.Show($"{Lang.T("saleOk")}\nInv: {res.InvoiceNumber}\nNet: {res.NetAmount:0.00}  {Lang.T("due")}: {res.DueAmount:0.00}  {Lang.T("change")}: {res.ChangeAmount:0.00}",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Receipt.Preview(text, res.InvoiceNumber);
            ClearAll(); await SearchAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Checkout failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
