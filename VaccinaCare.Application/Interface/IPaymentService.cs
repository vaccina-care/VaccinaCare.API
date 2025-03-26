using Microsoft.AspNetCore.Http;
using VaccinaCare.Domain.DTOs.PaymentDTOs;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Application.Interface;

public interface IPaymentService
{
    Task<string> GetPaymentUrl(Guid appointmentId, HttpContext context);

    Task<PaymentResponseModel> ProcessPaymentCallback(IQueryCollection query);

    Task<PaymentTransactionSummaryDto> GetPaymentTransactionSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        PaymentTransactionStatus? status = null);
}