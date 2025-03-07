using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IClaimsService _claimsService;
    private readonly IPayOsService _paymentService;

    public PaymentController(ILoggerService logger, IClaimsService claimsService, IPayOsService paymentService)
    {
        _logger = logger;
        _claimsService = claimsService;
        _paymentService = paymentService;
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResult<string>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Checkout(Guid appointmentId)
    {
        try
        {
            var paymentUrl = await _paymentService.ProcessPayment(appointmentId);
            return Ok(new { success = true, url = paymentUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] WebhookData webhookData)
    {
        return await _paymentService.PaymentWebhook(webhookData);
    }


}


