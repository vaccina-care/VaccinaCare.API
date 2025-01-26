namespace VaccinaCare.Domain.Entities;

public partial class AppointmentsVaccine : BaseEntity
{

    public Guid? AppointmentId { get; set; }

    public Guid? VaccineId { get; set; }

    public int? Quantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Vaccine? Vaccine { get; set; }
}
