namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class BookSingleVaccineRequestDto
{
    public Guid VaccineId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime StartDate { get; set; }
}
