namespace VaccinaCare.Domain.Enums;

public enum PaymentStatus
{
    Pending, // Đang chờ thanh toán
    Success, // Thanh toán thành công
    Failed, // Thanh toán thất bại
    Cancelled, // Người dùng đã hủy thanh toán
    Refunded // Đã hoàn tiền (trường hợp trả lại tiền cho người dùng)
}