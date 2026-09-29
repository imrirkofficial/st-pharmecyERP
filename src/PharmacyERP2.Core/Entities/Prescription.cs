using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyERP2.Core.Entities;

public class Prescription : BaseEntity
{
    [Key]
    public int PrescriptionId { get; set; }

    [Required, StringLength(50)]
    public string PrescriptionNumber { get; set; } = string.Empty;

    public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [StringLength(100)]
    public string? DoctorName { get; set; }

    public int? DoctorId { get; set; }
    [ForeignKey(nameof(DoctorId))]
    public Doctor? Doctor { get; set; }

    /// <summary>Scanned prescription image path.</summary>
    [StringLength(500)]
    public string? ImagePath { get; set; }

    [StringLength(200)]
    public string? Diagnosis { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsFulfilled { get; set; }
    public DateTime? FulfilledDate { get; set; }
    public int? FulfilledByUserId { get; set; }

    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}

public class PrescriptionItem
{
    [Key]
    public int PrescriptionItemId { get; set; }

    public int PrescriptionId { get; set; }
    [ForeignKey(nameof(PrescriptionId))]
    public Prescription Prescription { get; set; } = null!;

    public int MedicineId { get; set; }
    [ForeignKey(nameof(MedicineId))]
    public Medicine Medicine { get; set; } = null!;

    [StringLength(200)]
    public string Dosage { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public int Duration { get; set; }

    [StringLength(500)]
    public string? Instructions { get; set; }
}
