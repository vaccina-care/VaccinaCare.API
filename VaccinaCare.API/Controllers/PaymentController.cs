using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Domain.DTOs.PaymentDTOs;

namespace VaccinaCare.API.Controllers;

public class PaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;

    public PaymentController(IVnPayService vnPayService)
    {
        _vnPayService = vnPayService;
    }

    public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
    {
        var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

        return Redirect(url);
    }



    [HttpGet]
    public IActionResult PaymentCallbackVnpay()
    {
        var response = _vnPayService.PaymentExecute(Request.Query);
        return Ok(response);
    }


}