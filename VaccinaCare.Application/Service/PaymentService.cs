using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VNPAY.NET;

namespace VaccinaCare.Application.Service;

public class PaymentService : IPaymentService
{
    private readonly Vnpay _vnPay;
    private readonly ILoggerService _loggerService;

    public PaymentService(ILoggerService loggerService, Vnpay vnPay)
    {
        _loggerService = loggerService;
        _vnPay = vnPay;
    }
}