using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public partial class PaymentTransaction : BaseEntity
{
    public Guid PaymentId { get; set; } // Khóa ngoại đến Payment
    public string? TransactionId { get; set; } // mã giao dịch trả về từ VNPay
    public decimal? Amount { get; set; } // số tiền thanh toán
    public DateTime TransactionDate { get; set; } // thời gian thực hiện giao dịch
    public string? ResponseCode { get; set; } // mã phản hồi từ cổng thanh toán (00 thành công,...)
    public string? ResponseMessage { get; set; } // thông điệp từ cổng thanh toán
    public PaymentTransactionStatus Status { get; set; } // trạng thái giao dịch
    public string? Note { get; set; } // ghi chú bổ sung nếu cần

    // Navigation properties
    public virtual Payment Payment { get; set; }
}