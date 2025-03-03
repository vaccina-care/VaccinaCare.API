namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class GenerateAppointmentsRequest
{
    public List<Guid> VaccineIds { get; set; } = new();
    public Guid ChildId { get; set; }
    public DateTime StartDate { get; set; }
}