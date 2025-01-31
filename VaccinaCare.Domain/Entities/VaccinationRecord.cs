namespace VaccinaCare.Domain.Entities;

public partial class VaccinationRecord : BaseEntity
{
    public Guid? ChildId { get; set; }

    public Guid? VaccineId { get; set; }
    public DateTime? VaccinationDate { get; set; }
    public string? ReactionDetails { get; set; }
    public virtual Child? Child { get; set; }

    public virtual Vaccine? Vaccine { get; set; }
}

