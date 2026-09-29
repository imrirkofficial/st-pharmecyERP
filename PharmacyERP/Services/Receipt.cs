using System.Drawing.Printing;
using System.Text;

namespace PharmacyERP.Services;

/// <summary>32-col thermal receipt (Xprinter/POS-58) + PrintPreview. Bangla-safe (ASCII totals).</summary>
public static class Receipt
{
    public record ReceiptItem(string Name, int Qty, decimal Price);
    public record ReceiptPay(string Method, decimal Amount, string? TrxId);

    public static string Build(string invoice, DateTime date,
        List<ReceiptItem> items, decimal total, decimal discount, decimal net,
        List<ReceiptPay> pays, decimal due, decimal change, string? customer)
    {
        var s = BdSettings.Current;
        var b = new StringBuilder();
        void C(string t) => b.AppendLine(Center(t, 32));
        void L(string l, string r) => b.AppendLine(l.PadRight(20).Substring(0, Math.Min(20, l.Length)).PadRight(20) + r.PadLeft(12));
        C(s.PharmacyName);
        if (!string.IsNullOrWhiteSpace(s.Address)) C(s.Address);
        if (!string.IsNullOrWhiteSpace(s.Phone)) C("Mob: " + s.Phone);
        if (!string.IsNullOrWhiteSpace(s.DgdaLicense)) C("DGDA: " + s.DgdaLicense);
        b.AppendLine(new string('-', 32));
        b.AppendLine($"Inv: {invoice}");
        b.AppendLine($"Date: {date:yyyy-MM-dd HH:mm}");
        if (!string.IsNullOrWhiteSpace(customer)) b.AppendLine($"Cust: {customer}");
        if (BdSettings.IsMulti && !string.IsNullOrWhiteSpace(s.CurrentBranchName)) b.AppendLine($"Branch: {s.CurrentBranchName}");
        b.AppendLine(new string('-', 32));
        foreach (var i in items)
            L($"{i.Name} x{i.Qty}", (i.Qty * i.Price).ToString("0.00"));
        b.AppendLine(new string('-', 32));
        L("Total:", total.ToString("0.00"));
        L("Discount:", discount.ToString("0.00"));
        L("Net:", net.ToString("0.00"));
        foreach (var p in pays)
            L($"{p.Method}" + (string.IsNullOrWhiteSpace(p.TrxId) ? "" : $"({p.TrxId})"), p.Amount.ToString("0.00"));
        if (due > 0) L("DUE:", due.ToString("0.00"));
        if (change > 0) L("Change:", change.ToString("0.00"));
        b.AppendLine(new string('-', 32));
        if (due > 0) C("Baki thakle doya kore porishodh korun");
        C("Prescription niye ashun, sustho thakun");
        if (!string.IsNullOrWhiteSpace(s.TradeLicense)) C("Trade: " + s.TradeLicense);
        return b.ToString();
    }

    private static string Center(string t, int w)
    {
        t = t.Length > w ? t[..w] : t;
        return new string(' ', (w - t.Length) / 2) + t;
    }

    public static void Preview(string text, string title = "Receipt")
    {
        var doc = new PrintDocument();
        doc.DocumentName = title;
        doc.PrintPage += (_, e) =>
        {
            using var font = new Font("Consolas", 8.5f);
            e.Graphics!.DrawString(text, font, Brushes.Black, new PointF(10, 10));
            e.HasMorePages = false;
        };
        using var dlg = new PrintPreviewDialog { Document = doc, Width = 500, Height = 700 };
        dlg.ShowDialog();
    }

    public static void Print(string text, string title = "Receipt")
    {
        var doc = new PrintDocument { DocumentName = title };
        doc.PrintPage += (_, e) =>
        {
            using var font = new Font("Consolas", 8.5f);
            e.Graphics!.DrawString(text, font, Brushes.Black, new PointF(10, 10));
            e.HasMorePages = false;
        };
        doc.Print();
    }
}
