using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface.PaymentService;

namespace VaccinaCare.API.Controllers;

public class PaymentController : ControllerBase
{
	
    private readonly IVnPayService _vnPayService;
    
    public PaymentController(IVnPayService vnPayService)
    {
		
	    _vnPayService = vnPayService;
    }

    [HttpGet]
    public IActionResult PaymentCallbackVnpay()
    {
	    var response = _vnPayService.PaymentExecute(Request.Query);
	    return Ok(response);
    }


}