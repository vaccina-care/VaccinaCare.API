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

            return Ok(new { url = paymentUrl });
        }
        catch (ArgumentException ex)
        {
            _logger.Error($"Lỗi khi tạo URL thanh toán: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi server khi tạo URL thanh toán: {ex.Message}");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi xử lý thanh toán." });
        }
    }
}