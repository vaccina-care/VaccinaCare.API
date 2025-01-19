namespace VaccinaCare.Domain.Entities;

public partial class VaccineSuggestion : BaseEntity
{

    public int? ChildId { get; set; }

    public int? VaccineId { get; set; }

    public string? SuggestedVaccine { get; set; }

    public string? Status { get; set; }

    public virtual Child? Child { get; set; }

    public virtual Vaccine? Vaccine { get; set; }
}
