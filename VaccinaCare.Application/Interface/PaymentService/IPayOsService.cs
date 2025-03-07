using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

namespace VaccinaCare.Application.Interface.PaymentService;

public interface IPayOsService
{
    Task<string> ProcessPayment(Guid appointmentId);
    Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData);
}