using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public class Child : BaseEntity
{
    public Guid? ParentId { get; set; }
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool Gender { get; set; }
    public string? MedicalHistory { get; set; }

    public BloodType? BloodType { get; set; } // Nhóm máu

    public bool HasChronicIllnesses { get; set; } // Có bệnh mãn tính không
    public string? ChronicIllnessesDescription { get; set; } // Mô tả bệnh mãn tính

    public bool HasAllergies { get; set; } // Có dị ứng không
    public string? AllergiesDescription { get; set; } // Mô tả dị ứng

    public bool HasRecentMedication { get; set; } // Có dùng thuốc gần đây không
    public string? RecentMedicationDescription { get; set; } // Mô tả thuốc đang dùng

    public bool HasOtherSpecialCondition { get; set; } // Có triệu chứng đặc biệt không
    public string? OtherSpecialConditionDescription { get; set; } // Mô tả triệu chứng

    public virtual User? Parent { get; set; }

    public virtual ICollection<PackageProgress> PackageProgresses { get; set; } = new List<PackageProgress>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
}