namespace VaccinaCare.Domain.Entities;

public partial class Child : BaseEntity
{
    public int? ParentId { get; set; } // ID của phụ huynh

    public string? FullName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? MedicalHistory { get; set; }

    public virtual User? Parent { get; set; } // Tham chiếu tới phụ huynh

    public virtual ICollection<PackageProgress> PackageProgresses { get; set; } = new List<PackageProgress>();

    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();

    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();

}

