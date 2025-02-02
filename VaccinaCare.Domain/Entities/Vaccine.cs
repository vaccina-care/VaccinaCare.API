using System.ComponentModel.DataAnnotations.Schema;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public partial class Vaccine : BaseEntity
{
    [Column(TypeName = "nvarchar(255)")]
    public string? VaccineName { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? PicUrl { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? Type { get; set; }

    public decimal? Price { get; set; }

    public BloodType? ForBloodType { get; set; } 

    public bool? AvoidChronic { get; set; } // Không khuyến khích cho bệnh mãn tính

    public bool? AvoidAllergy { get; set; } // Không khuyến nghị cho dị ứng

    public bool? HasDrugInteraction { get; set; } // Có cảnh báo về tương tác thuốc không?

    public bool? HasSpecialWarning { get; set; } // Có cảnh báo điều kiện sức khỏe đặc biệt không?

    public virtual ICollection<AppointmentsVaccine> AppointmentsVaccines { get; set; } = new List<AppointmentsVaccine>();
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
    public virtual ICollection<VaccineAvailability> VaccineAvailabilities { get; set; } = new List<VaccineAvailability>();
    public virtual ICollection<UsersVaccination> UsersVaccinations { get; set; } = new List<UsersVaccination>();
    public virtual ICollection<VaccinePackageDetail> VaccinePackageDetails { get; set; } = new List<VaccinePackageDetail>();
    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();
}

