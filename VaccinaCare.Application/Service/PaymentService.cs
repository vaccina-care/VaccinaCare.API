using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;

namespace VaccinaCare.Application.Service;

public class PaymentService : IPaymentService
{
    private readonly IVnpay _vnPay;
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IClaimsService _claimsService;

    public PaymentService(ILoggerService loggerService, IUnitOfWork unitOfWork, IVnpay vnPay,
        IConfiguration configuration, IClaimsService claimsService)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _claimsService = claimsService;
        _vnPay.Initialize(_configuration["Vnpay:TmnCode"], _configuration["Vnpay:HashSecret"],
            _configuration["Vnpay:BaseUrl"], _configuration["Vnpay:CallbackUrl"]);
    }

    public string CreatePaymentUrl(double moneyToPay, string description)
    {
        try
        {
            var ipAddress = _claimsService.IpAddress;
            var request = new PaymentRequest
            {
                PaymentId = DateTime.Now.Ticks,
                Money = moneyToPay,
                Description = description,
                IpAddress = ipAddress,
                BankCode = BankCode.ANY, // Tùy chọn. Mặc định là tất cả phương thức giao dịch
                CreatedDate = DateTime.Now, // Tùy chọn. Mặc định là thời điểm hiện tại
                Currency = Currency.VND, // Tùy chọn. Mặc định là VND (Việt Nam đồng)
                Language = DisplayLanguage.Vietnamese // Tùy chọn. Mặc định là tiếng Việt
            };

            var paymentUrl = _vnPay.GetPaymentUrl(request);

            return paymentUrl;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    
}