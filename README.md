# Pharmacy ERP System

A comprehensive Desktop ERP application for pharmacy management built with .NET 8.0 and Windows Forms.

## Features

### 1. **Inventory Management**
- Add, edit, and delete medicine records
- Track stock levels with reorder alerts
- Monitor expiry dates
- Barcode management
- Organize by category and manufacturer
- Shelf location tracking

### 2. **Sales & Billing (POS)**
- Quick medicine search
- Real-time invoice generation
- Multiple payment methods (Cash, Card, Mobile Banking)
- Customer linking
- Discount management
- Receipt printing

### 3. **Purchase Management**
- Create purchase orders
- Track suppliers
- Stock receiving
- Batch and expiry tracking
- Payment status monitoring
- Automatic inventory updates

### 4. **Supplier Management**
- Complete supplier database
- Contact information
- Purchase history
- Outstanding balance tracking
- Performance analytics

### 5. **Customer & Prescription Management**
- Customer profiles with medical history
- Allergy tracking
- Prescription management
- Doctor information
- Purchase history
- Dosage and instruction tracking

### 6. **Reporting**
- Daily sales reports
- Stock reports
- Purchase analysis
- Customer analytics
- Profit calculations

## Technology Stack

- **Framework**: .NET 8.0
- **UI**: Windows Forms
- **Database**: SQL Server with Entity Framework Core 8.0
- **ORM**: Entity Framework Core
- **Language**: C# 12

## Database Schema

### Core Tables
- **Users** - System user accounts and authentication
- **Medicines** - Medicine inventory with full details
- **Customers** - Customer profiles and medical history
- **Suppliers** - Supplier information
- **Sales & SaleItems** - Sales transactions
- **Purchases & PurchaseItems** - Purchase orders
- **Prescriptions & PrescriptionItems** - Prescription management

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK
- SQL Server 2019 or later (or SQL Server LocalDB)
- Visual Studio 2022 (recommended)

### Installation

1. **Clone or extract the project**
   ```bash
   cd D:\PharmacyERP2
   ```

2. **Open the solution**
   ```
   Open PharmacyERP.sln in Visual Studio
   ```

3. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

4. **Update database connection string**
   
   Edit `PharmacyERP\Data\PharmacyDbContext.cs` if needed to match your SQL Server instance.

5. **Create database**
   ```bash
   dotnet ef database update
   ```
   
   Or in Package Manager Console:
   ```
   Update-Database
   ```

6. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```

### Default Login Credentials
- **Username**: admin
- **Password**: admin123

⚠️ **Important**: Change the default password after first login in a production environment.

## Project Structure

```
PharmacyERP2/
├── PharmacyERP/
│   ├── Models/              # Data models
│   │   ├── User.cs
│   │   ├── Medicine.cs
│   │   ├── Customer.cs
│   │   ├── Supplier.cs
│   │   ├── Sale.cs
│   │   ├── Purchase.cs
│   │   └── Prescription.cs
│   ├── Data/               # Database context
│   │   └── PharmacyDbContext.cs
│   ├── Forms/              # UI Forms
│   │   ├── LoginForm.cs
│   │   └── MainForm.cs
│   ├── Program.cs          # Entry point
│   └── appsettings.json    # Configuration
└── PharmacyERP.sln         # Solution file
```

## Configuration

### Database Configuration
Update the connection string in `PharmacyDbContext.cs`:
```csharp
optionsBuilder.UseSqlServer(
    @"Server=(localdb)\mssqllocaldb;Database=PharmacyERPDB;Trusted_Connection=True;");
```

### Application Settings
Edit `appsettings.json` for application-wide settings:
```json
{
  "AppSettings": {
    "PharmacyName": "Your Pharmacy Name",
    "Currency": "BDT"
  }
}
```

## Security Notes

⚠️ **This is a development version**. For production use:

1. **Implement password hashing** - Currently passwords are stored in plain text
2. **Add SSL/TLS** for database connections
3. **Implement role-based access control**
4. **Add audit logging**
5. **Implement data encryption** for sensitive information
6. **Regular backups** of the database

## Future Enhancements

- [ ] Password encryption with BCrypt
- [ ] Barcode scanner integration
- [ ] Receipt printer integration
- [ ] SMS notifications for customers
- [ ] Advanced reporting with charts
- [ ] Data export to Excel/PDF
- [ ] Multi-branch support
- [ ] Cloud backup integration
- [ ] Mobile app integration
- [ ] GST/VAT calculation
- [ ] Automated reordering
- [ ] Email notifications

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server is running
- Check connection string in PharmacyDbContext.cs
- Verify Windows authentication or use SQL authentication

### Migration Issues
```bash
# Reset and recreate database
dotnet ef database drop
dotnet ef database update
```

## License

This project is provided as-is for educational and commercial use.

## Support

For issues or questions about this ERP system, please refer to the documentation or contact your system administrator.

## Version

**Current Version**: 1.0.0
**Last Updated**: September 2026

---

**Built with ❤️ for modern pharmacy management**
