namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class CreateAppointmentDto
{
    public List<Guid> VaccineIds { get; set; } = new List<Guid>(); // Danh sách vaccine cần đặt lịch
    public Guid ChildId { get; set; }
    public DateTime StartDate { get; set; } // Ngày bắt đầu tiêm chủng
}
