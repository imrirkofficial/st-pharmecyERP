using System.ComponentModel.DataAnnotations;
using PharmacyERP2.Core.Enums;

namespace PharmacyERP2.Core.Entities;

public class User : BaseEntity
{
    [Key]
    public int UserId { get; set; }

    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt hash. Legacy plaintext migrated on first login.</summary>
    [Required, StringLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(50)]
    public string Role { get; set; } = Roles.User;

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public DateTime? LastLoginDate { get; set; }
}
