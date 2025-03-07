using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/Vnpay")]
public class PaymentController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IClaimsService _claimsService;

    public PaymentController(ILoggerService logger, IClaimsService claimsService)
    {
        _logger = logger;
        _claimsService = claimsService;
    }



}


