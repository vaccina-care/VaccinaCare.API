namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class CreateAppointmentSingleVaccineDto
{
    public Guid VaccineId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime StartDate { get; set; } // Ngày bắt đầu tiêm chủng
}