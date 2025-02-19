namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class BookConsultationDto
{
    public Guid ChildId { get; set; }
    public DateTime AppointmentDate { get; set; }
}