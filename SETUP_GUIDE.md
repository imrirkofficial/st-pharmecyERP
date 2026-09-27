# Pharmacy ERP - Setup Guide

## দ্রুত শুরু করার নির্দেশিকা (Quick Start Guide in Bengali)

### প্রয়োজনীয় সফটওয়্যার (Required Software)

1. **Visual Studio 2022** (Community Edition - ফ্রি)
   - Download: https://visualstudio.microsoft.com/downloads/

2. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0

3. **SQL Server 2019+** অথবা **SQL Server Express** (ফ্রি)
   - Download: https://www.microsoft.com/sql-server/sql-server-downloads

### ইনস্টলেশন ধাপসমূহ (Installation Steps)

#### ধাপ ১: Visual Studio ইনস্টল করুন

1. Visual Studio 2022 ডাউনলোড করুন
2. ইনস্টলারে নিচের workloads সিলেক্ট করুন:
   - ✅ .NET desktop development
   - ✅ Data storage and processing

#### ধাপ ২: SQL Server ইনস্টল করুন

1. SQL Server Express ডাউনলোড করুন
2. ইনস্টল করার সময়:
   - Authentication Mode: **Windows Authentication** সিলেক্ট করুন
   - অথবা Mixed Mode সিলেক্ট করে একটি sa password দিন

#### ধাপ ৩: প্রজেক্ট ওপেন করুন

1. File Explorer এ যান: `D:\PharmacyERP2`
2. `PharmacyERP.sln` ফাইলে ডাবল ক্লিক করুন
3. Visual Studio তে প্রজেক্ট ওপেন হবে

#### ধাপ ৪: NuGet Packages রিস্টোর করুন

Visual Studio তে:
- Solution Explorer → Right-click on solution → "Restore NuGet Packages"

অথবা Package Manager Console এ:
```
Update-Package -reinstall
```

#### ধাপ ৫: Database তৈরি করুন

**Package Manager Console** এ (View → Other Windows → Package Manager Console):

```powershell
Add-Migration InitialCreate
Update-Database
```

#### ধাপ ৬: Run করুন

- `F5` প্রেস করুন অথবা "Start" বাটনে ক্লিক করুন
- Login Form দেখাবে

### ডিফল্ট লগইন (Default Login)

```
Username: admin
Password: admin123
```

---

## সমস্যা সমাধান (Troubleshooting)

### সমস্যা ১: Database Connection Error

**Error**: "Cannot connect to database"

**সমাধান**:
1. SQL Server চালু আছে কিনা চেক করুন:
   - Services → SQL Server (MSSQLLOCALDB) → Start

2. Connection String চেক করুন:
   - `PharmacyDbContext.cs` ফাইল ওপেন করুন
   - Connection string আপডেট করুন:

```csharp
// For LocalDB
optionsBuilder.UseSqlServer(
    @"Server=(localdb)\mssqllocaldb;Database=PharmacyERPDB;Trusted_Connection=True;");

// অথবা SQL Server Express এর জন্য
optionsBuilder.UseSqlServer(
    @"Server=localhost\SQLEXPRESS;Database=PharmacyERPDB;Trusted_Connection=True;");
```

### সমস্যা ২: Migration Error

**সমাধান**:
```powershell
# সব migration মুছে নতুন করে তৈরি করুন
Remove-Migration
Add-Migration InitialCreate
Update-Database
```

### সমস্যা ৩: NuGet Package Error

**সমাধান**:
```powershell
# Package Manager Console এ
Update-Package -reinstall
```

---

## কনফিগারেশন (Configuration)

### ১. Database Configuration

`PharmacyERP\Data\PharmacyDbContext.cs`:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // আপনার SQL Server instance অনুযায়ী পরিবর্তন করুন
    optionsBuilder.UseSqlServer(
        @"Server=YOUR_SERVER;Database=PharmacyERPDB;Trusted_Connection=True;");
}
```

### ২. Application Settings

`appsettings.json`:

```json
{
  "AppSettings": {
    "PharmacyName": "আপনার ফার্মেসির নাম",
    "Currency": "BDT",
    "Version": "1.0.0"
  }
}
```

---

## ডাটাবেস স্ট্রাকচার (Database Structure)

### মূল টেবিলসমূহ (Main Tables)

1. **Users** - ইউজার একাউন্ট
2. **Medicines** - ঔষধের তথ্য
3. **Customers** - কাস্টমার তথ্য
4. **Suppliers** - সাপ্লায়ার তথ্য
5. **Sales** / **SaleItems** - বিক্রয় তথ্য
6. **Purchases** / **PurchaseItems** - ক্রয় তথ্য
7. **Prescriptions** / **PrescriptionItems** - প্রেসক্রিপশন

---

## পরবর্তী ধাপসমূহ (Next Steps)

### ১. প্রথম লগইন করুন
- Username: `admin`
- Password: `admin123`

### ২. Admin Password পরিবর্তন করুন
- Settings → User Management → Change Password

### ৩. আপনার ডাটা এন্ট্রি শুরু করুন:

**প্রথমে:**
1. Suppliers যোগ করুন
2. Medicine categories যোগ করুন
3. Medicines যোগ করুন

**তারপর:**
1. Customers যোগ করুন
2. Sales/Purchase শুরু করুন

---

## ব্যাকআপ (Backup)

### ম্যানুয়াল ব্যাকআপ

SQL Server Management Studio (SSMS) এ:

```sql
BACKUP DATABASE PharmacyERPDB
TO DISK = 'D:\Backups\PharmacyERP_Backup.bak'
WITH FORMAT;
```

### Automated Backup

SQL Server Agent দিয়ে শিডিউল করুন:
- Daily backup at 12:00 AM
- Location: `D:\Backups\`

---

## সাপোর্ট (Support)

এই ERP সিস্টেম সম্পর্কে কোন প্রশ্ন থাকলে:

1. README.md ফাইল দেখুন
2. Database documentation চেক করুন
3. আপনার সিস্টেম এডমিনিস্ট্রেটরের সাথে যোগাযোগ করুন

---

**Version**: 1.0.0  
**Last Updated**: September 2026  
**Language Support**: English, বাংলা
