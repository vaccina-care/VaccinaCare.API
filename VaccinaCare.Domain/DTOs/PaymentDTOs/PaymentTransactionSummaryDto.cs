namespace VaccinaCare.Domain.DTOs.PaymentDTOs;

public class PaymentTransactionSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalCompletedAppointments { get; set; }
}