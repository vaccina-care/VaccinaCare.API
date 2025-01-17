namespace VaccinaCare.Domain.Entities;

public partial class AppointmentsService : BaseEntity
{

    public int? AppointmentId { get; set; }

    public int? ServiceId { get; set; }

    public int? Quantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Service? Service { get; set; }
}
