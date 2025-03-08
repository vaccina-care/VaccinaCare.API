using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.PaymentDTOs;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly IPaymentService _paymentService;

    public PaymentController(IVnPayService vnPayService, IPaymentService paymentService)
    {
        _vnPayService = vnPayService;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCallbackVnpay()
    {
        try
        {
            var response = await _paymentService.ProcessPaymentCallback(Request.Query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    
    [HttpGet("checkout/{appointmentId}")]
    [ProducesResponseType(typeof(ApiResult<string>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> CheckoutPayment(Guid appointmentId)
    {
        try
        {
            var paymentUrl = await _paymentService.GetPaymentUrl(appointmentId, HttpContext);

            if (string.IsNullOrEmpty(paymentUrl))
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Unable to create payment URL, please check the appointment details and try again."
                });
            return Ok(new ApiResult<string>
            {
                IsSuccess = true,
                Data = paymentUrl,
                Message = "Payment URL created successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = $"An error occurred while processing your request: {ex.Message}"
            });
        }
    }
    
    

}