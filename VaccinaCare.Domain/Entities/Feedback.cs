namespace VaccinaCare.Domain.Entities;

public partial class Feedback : BaseEntity
{
    public Guid? AppointmentId { get; set; }
    public int? Rating { get; set; }
    public string? Comments { get; set; }
    public virtual Appointment? Appointment { get; set; } // Tham chiếu đến Appointment

}
