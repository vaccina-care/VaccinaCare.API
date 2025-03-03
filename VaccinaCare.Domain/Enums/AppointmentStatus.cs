namespace VaccinaCare.Domain.Enums;

public enum AppointmentStatus
{
    Pending, // Chờ xác nhận
    Confirmed, // Đã xác nhận
    Completed, // Đã hoàn thành
    Cancelled, // Đã hủy
    Rescheduled // Đã dời lịch
}