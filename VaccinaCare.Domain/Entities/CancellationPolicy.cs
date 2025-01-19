namespace VaccinaCare.Domain.Entities;

public partial class CancellationPolicy : BaseEntity
{
    public string? PolicyName { get; set; }
    public string? Description { get; set; }
    public int? CancellationDeadline { get; set; }
    public decimal? PenaltyFee { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

