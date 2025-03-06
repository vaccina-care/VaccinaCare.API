using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/VnPay")]
public class PaymentController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IClaimsService _claimsService;
    private readonly IPaymentService _paymentService;

    public PaymentController(ILoggerService logger, IClaimsService claimsService, IPaymentService paymentService)
    {
        _logger = logger;
        _claimsService = claimsService;
        _paymentService = paymentService;
    }

    [HttpGet("CreatePaymentUrl")]
    public async Task<IActionResult> CreatePaymentUrl([FromQuery] Guid appointmentId)
    {
        try
        {
            if (appointmentId == Guid.Empty)
                return BadRequest("AppointmentId không hợp lệ.");

            var paymentUrl = await _paymentService.CreatePaymentUrl(appointmentId);

            return Created(paymentUrl, paymentUrl);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
    
    
   