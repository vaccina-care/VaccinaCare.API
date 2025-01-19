namespace VaccinaCare.Domain.Entities;

public partial class AppointmentsVaccine : BaseEntity
{

    public int? AppointmentId { get; set; }

    public int? VaccineId { get; set; }

    public int? Quantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Vaccine? Vaccine { get; set; }
}
