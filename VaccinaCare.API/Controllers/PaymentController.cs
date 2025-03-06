using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/Vnpay")]
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
    
    [HttpGet("IpnAction")]
    public async Task<IActionResult> IpnAction([FromQuery] IQueryCollection parameters)
    {
        try
        {
            if (parameters == null || !parameters.Any())
            {
                return BadRequest("Invalid IPN request.");
            }

            // Call the PaymentService to handle the IPN data
            await _paymentService.HandleIpnNotification(parameters);
        
            return Ok("IPN handled successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing IPN request: {ex.Message}");
            return BadRequest("Error processing IPN request.");
        }
    }

    
}
    
    
   