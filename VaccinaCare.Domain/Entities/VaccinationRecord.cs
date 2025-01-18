namespace VaccinaCare.Domain.Entities;

public partial class VaccinationRecord : BaseEntity
{

    public int? ChildId { get; set; }

    public DateTime? VaccinationDate { get; set; }

    public string? ReactionDetails { get; set; }

    public virtual Child? Children { get; set; }

}
