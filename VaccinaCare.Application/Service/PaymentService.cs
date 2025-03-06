using System.Collections.Concurrent;
using Curus.Service.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VnPayDTOs;
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
    private readonly ILoggerService _loggerService;
    private readonly IVnpay _vnpay;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(ILoggerService loggerService, IUnitOfWork unitOfWork, IVnpay vnpay)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
        _vnpay = vnpay;
    }

    public async Task<string> CreatePaymentUrl(Guid appointmentId)
    {
        // Lấy thông tin Appointment và bao gồm các thuộc tính liên quan
        var appointment = await _unitOfWork.AppointmentRepository
            .FirstOrDefaultAsync(a => a.Id == appointmentId,
                a => a.AppointmentsVaccines,
                a => a.Child); // Đảm bảo bao gồm thông tin về Child.

        if (appointment == null)
        {
            _loggerService.Error($"Lịch hẹn không tồn tại cho AppointmentId: {appointmentId}");
            throw new ArgumentException("Lịch hẹn không tồn tại.");
        }

        var appointmentVaccine = appointment.AppointmentsVaccines.FirstOrDefault(); // Lấy mũi tiêm đầu tiên

        // Lấy tổng giá tiền của mũi tiêm và tính toán số tiền cọc (20%)
        double totalPrice = (double)(appointmentVaccine.TotalPrice ?? 0); // Convert từ decimal sang double
        double totalDeposit = totalPrice * 0.20; // Tính 20% số tiền cọc

        HttpContext httpContext = new DefaultHttpContext();
        var ipAddress = NetworkHelper.GetIpAddress(httpContext); // Lấy địa chỉ IP của thiết bị thực hiện giao dịch

        var request = new PaymentRequest
        {
            PaymentId = DateTime.Now.Ticks,
            Money = totalDeposit, // Sử dụng totalDeposit là kiểu double
            Description = $"thanh toan dat coc {appointment.VaccineType}",
            IpAddress = ipAddress,
            BankCode = BankCode.ANY, // Tùy chọn. Mặc định là tất cả phương thức giao dịch
            CreatedDate = DateTime.Now, // Tùy chọn. Mặc định là thời điểm hiện tại
            Currency = Currency.VND, // Tùy chọn. Mặc định là VND (Việt Nam đồng)
            Language = DisplayLanguage.Vietnamese // Tùy chọn. Mặc định là tiếng Việt
        };

        var paymentUrl = _vnpay.GetPaymentUrl(request);
        return paymentUrl;
    }
    
    
}