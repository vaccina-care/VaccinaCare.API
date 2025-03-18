using Microsoft.AspNetCore.Http;
using VaccinaCare.Domain.DTOs.PaymentDTOs;

namespace VaccinaCare.Application.Interface;

public interface IPaymentService
{
    Task<string> GetPaymentUrl(Guid appointmentId, HttpContext context);

    Task<PaymentResponseModel> ProcessPaymentCallback(IQueryCollection query);
}