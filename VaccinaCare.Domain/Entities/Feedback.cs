namespace VaccinaCare.Domain.Entities;

public class Feedback : BaseEntity
{
    public Guid? AppointmentId { get; set; }
    public Guid? UserId { get; set; }
    public int? Rating { get; set; }
    public string? Comments { get; set; }
    public virtual Appointment? Appointment { get; set; } // Tham chiếu đến Appointment
    public virtual User? User { get; set; } // Tham chiếu đến
}