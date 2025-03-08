namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class CreateAppointmentDto
{
    public Guid VaccineId { get; set; } = new(); // Danh sách vaccine cần đặt lịch
    public Guid ChildId { get; set; }
    public DateTime StartDate { get; set; } // Ngày bắt đầu tiêm chủng
}