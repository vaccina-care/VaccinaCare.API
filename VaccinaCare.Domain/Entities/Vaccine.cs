using System.ComponentModel.DataAnnotations.Schema;

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

    [Column(TypeName = "nvarchar(50)")]
    public string? ForBloodType { get; set; } // Nhóm máu phù hợp

    [Column(TypeName = "nvarchar(max)")]
    public string? AvoidChronic { get; set; } // Không khuyến khích cho bệnh mãn tính

    [Column(TypeName = "nvarchar(max)")]
    public string? AvoidAllergy { get; set; } // Không khuyến nghị cho dị ứng

    [Column(TypeName = "nvarchar(max)")]
    public string? DrugInteraction { get; set; } // Cảnh báo về tương tác thuốc

    [Column(TypeName = "nvarchar(max)")]
    public string? SpecialWarn { get; set; } // Cảnh báo điều kiện sức khỏe đặc biệt

    public virtual ICollection<AppointmentsVaccine> AppointmentsVaccines { get; set; } = new List<AppointmentsVaccine>();
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
    public virtual ICollection<VaccineAvailability> VaccineAvailabilities { get; set; } = new List<VaccineAvailability>();
    public virtual ICollection<UsersVaccination> UsersVaccinations { get; set; } = new List<UsersVaccination>();
    public virtual ICollection<VaccinePackageDetail> VaccinePackageDetails { get; set; } = new List<VaccinePackageDetail>();
    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();
}
