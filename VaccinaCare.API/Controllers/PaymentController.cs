using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Application.Ultils;

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
                    Data = null,
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

    [HttpGet("callback")]
    public async Task<IActionResult> PaymentCallbackVnpay()
    {
        try
        {
            // Lấy dữ liệu callback từ VNPay
            var response = await _paymentService.ProcessPaymentCallback(Request.Query);

            // Kiểm tra kết quả thanh toán từ VNPay và cập nhật transaction vào database
            if (response.Success)
                // Ghi nhận giao dịch thành công vào database và chuyển hướng tới frontend
                return Redirect(
                    $"https://vaccina-care-fe.vercel.app/payment-success?orderId={response.OrderId}&transactionid={response.TransactionId}");
            else
                // Nếu thanh toán thất bại, chuyển hướng đến frontend thông báo thất bại
                return Redirect("https://vaccina-care-fe.vercel.app/payment-fail");
        }
        catch (Exception ex)
        {
            // Nếu có lỗi, chuyển hướng đến frontend trang thất bại
            return Redirect("https://vaccina-care-fe.vercel.app/payment-fail");
        }
    }
}