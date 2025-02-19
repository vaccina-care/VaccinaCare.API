namespace VaccinaCare.Domain.Entities;

public class AppointmentVaccineSuggestions : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid VaccineSuggestionId { get; set; }

    public virtual Appointment Appointment { get; set; }
    public virtual VaccineSuggestion VaccineSuggestion { get; set; }
}
