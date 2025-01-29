using System.ComponentModel.DataAnnotations.Schema;

namespace VaccinaCare.Domain.Entities;

public partial class Vaccine : BaseEntity
{
    [Column(TypeName = "nvarchar(255)")] public string? VaccineName { get; set; }

    [Column(TypeName = "nvarchar(max)")] public string? Description { get; set; }

    [Column(TypeName = "nvarchar(500)")] public string? PicUrl { get; set; }

    [Column(TypeName = "nvarchar(100)")] public string? Type { get; set; }

    public decimal? Price { get; set; }

    public virtual ICollection<AppointmentsVaccine> AppointmentsVaccines { get; set; } =
        new List<AppointmentsVaccine>();

    public virtual ICollection<VaccineAvailability> VaccineAvailabilities { get; set; } =
        new List<VaccineAvailability>();

    public virtual ICollection<UsersVaccination> UsersVaccinations { get; set; } = new List<UsersVaccination>();

    public virtual ICollection<VaccinePackageDetail> VaccinePackageDetails { get; set; } =
        new List<VaccinePackageDetail>();

    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();
}