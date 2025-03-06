using Microsoft.AspNetCore.Http;

namespace VaccinaCare.Application.Interface;

public interface IPaymentService
{
    Task<string> CreatePaymentUrl(Guid appointmentId);
    
}