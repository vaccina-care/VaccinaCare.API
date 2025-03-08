using Microsoft.AspNetCore.Http;
using VaccinaCare.Domain.DTOs.PaymentDTOs;

namespace VaccinaCare.Application.Interface.PaymentService;

public interface IVnPayService
{
    string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
    PaymentResponseModel PaymentExecute(IQueryCollection collections);
}