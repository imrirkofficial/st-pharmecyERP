namespace PharmacyERP2.Core.Enums;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Pharmacist = "Pharmacist";
    public const string Cashier = "Cashier";
    public const string User = "User";
}

public static class PaymentMethod
{
    public const string Cash = "Cash";
    public const string Card = "Card";
    public const string MobileBanking = "Mobile Banking";
}

public static class PaymentStatus
{
    public const string Paid = "Paid";
    public const string Partial = "Partial";
    public const string Pending = "Pending";
}
