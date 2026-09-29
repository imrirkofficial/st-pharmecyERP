namespace PharmacyERP.Services;

/// <summary>Bangla / English UI strings. Toggle from Settings.</summary>
public static class Lang
{
    private static readonly Dictionary<string, (string En, string Bn)> _t = new()
    {
        ["dashboard"] = ("Dashboard", "ড্যাশবোর্ড"),
        ["medicines"] = ("Medicines", "ঔষধ"),
        ["salesPos"] = ("Sales / POS", "বিক্রি / POS"),
        ["purchase"] = ("Purchase", "ক্রয়"),
        ["suppliers"] = ("Suppliers", "সাপ্লায়ার"),
        ["customers"] = ("Customers", "কাস্টমার"),
        ["prescriptions"] = ("Prescriptions", "প্রেসক্রিপশন"),
        ["reports"] = ("Reports", "রিপোর্ট"),
        ["dueKhata"] = ("Due Khata", "বাকির খাতা"),
        ["returns"] = ("Returns", "ফেরত"),
        ["closing"] = ("Day Closing", "দিনের হিসাব"),
        ["settings"] = ("Settings", "সেটিংস"),
        ["logout"] = ("Logout", "লগআউট"),
        ["search"] = ("Search", "খুঁজুন"),
        ["checkout"] = ("Checkout", "বিল করুন"),
        ["total"] = ("Total", "মোট"),
        ["discount"] = ("Discount", "ছাড়"),
        ["paid"] = ("Paid", "পরিশোধ"),
        ["due"] = ("Due", "বাকি"),
        ["net"] = ("Net", "নিট"),
        ["change"] = ("Change", "ফেরত"),
        ["save"] = ("Save", "সেভ"),
        ["refresh"] = ("Refresh", "রিফ্রেশ"),
        ["collect"] = ("Collect", "আদায়"),
        ["print"] = ("Print", "প্রিন্ট"),
        ["preview"] = ("Preview", "প্রিভিউ"),
        ["close"] = ("Close", "বন্ধ"),
        ["todaySales"] = ("Today Sales", "আজকের বিক্রি"),
        ["profit"] = ("Profit", "লাভ"),
        ["lowStock"] = ("Low Stock", "কম স্টক"),
        ["expirySoon"] = ("Expiry Soon", "মেয়াদ শেষ হবে"),
        ["topSelling"] = ("Top Selling", "বেশি বিক্রি"),
        ["recentSales"] = ("Recent Sales", "সাম্প্রতিক বিক্রি"),
        ["cartEmpty"] = ("Cart empty.", "কার্ট খালি।"),
        ["saleOk"] = ("Sale OK!", "বিক্রি সফল!"),
        ["controlledWarn"] = ("Controlled drug! Verify prescription.", "কন্ট্রোলড ঔষধ! প্রেসক্রিপশন যাচাই করুন।"),
    };

    public static string T(string key)
    {
        if (!_t.TryGetValue(key, out var v)) return key;
        return BdSettings.Current.Language == "bn" ? v.Bn : v.En;
    }
}
