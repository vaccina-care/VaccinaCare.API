namespace VaccinaCare.Domain.Entities;

public partial class Notification : BaseEntity
{

    public int? AppointmentId { get; set; }

    public string? Message { get; set; }

    public string? ReadStatus { get; set; }

    public virtual Appointment? Appointment { get; set; }
}
