namespace VaccinaCare.Domain.DTOs.PaymentDTOs;

public class CreatePaymentRequest
{
    public Guid PaymentId { get; set; }
    public string? ReturnUrl { get; set; } = "https://vaccina-care-fe.vercel.app";
    public string? PaymentMethod { get; set; } = "VNPAY";
}