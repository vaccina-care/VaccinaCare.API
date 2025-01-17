namespace VaccinaCare.Domain.Entities;

public partial class Feedback : BaseEntity
{

    public int? AppointmentId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }
}
