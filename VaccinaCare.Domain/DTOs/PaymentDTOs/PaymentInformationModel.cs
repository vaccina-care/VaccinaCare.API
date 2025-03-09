namespace VaccinaCare.Domain.DTOs.PaymentDTOs;

public class PaymentInformationModel
{
    public string OrderType { get; set; }
    public double Amount { get; set; }
    public string OrderDescription { get; set; }
    public string Name { get; set; }
    public string OrderId { get; set; }

    // URL Callback để VNPay gửi dữ liệu thanh toán về Backend
    public string PaymentCallbackUrl { get; set; }
}