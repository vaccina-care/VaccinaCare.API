namespace VaccinaCare.Domain.Entities;

public partial class VaccineSuggestion : BaseEntity
{
    public Guid? ChildId { get; set; }

    public Guid? VaccineId { get; set; }
    public string? SuggestedVaccine { get; set; }
    public string? Status { get; set; }
    public virtual Child? Child { get; set; }

    public virtual Vaccine? Vaccine { get; set; }

    public virtual ICollection<AppointmentVaccineSuggestions> AppointmentVaccineSuggestions { get; set; } =
        new List<AppointmentVaccineSuggestions>();
}