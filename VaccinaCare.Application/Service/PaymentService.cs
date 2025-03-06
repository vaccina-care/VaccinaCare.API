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

        var ipAddress = "14.191.94.144"; 

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

    public async Task HandleIpnNotification(IQueryCollection parameters)
    {
        try
        {
            // Call VNPay's GetPaymentResult to validate the transaction
            var paymentResult = _vnpay.GetPaymentResult(parameters);

            if (paymentResult == null)
            {
                _loggerService.Error("Không tìm thấy kết quả thanh toán từ VNPay");
                return;
            }

            // Check if the payment was successful (based on ResponseCode and TransactionStatus)
            if (!paymentResult.IsSuccess)
            {
                _loggerService.Error($"Thanh toán không thành công: {paymentResult.Description}");
                return;
            }

            // Create a GUID from the long value (if conversion is possible)
            var paymentIdGuid = new Guid(paymentResult.PaymentId.ToString().PadLeft(32, '0'));

            var payment = await _unitOfWork.PaymentRepository
                .FirstOrDefaultAsync(p => p.Id == paymentIdGuid);


            if (payment == null)
            {
                _loggerService.Error($"Không tìm thấy Payment với PaymentId: {paymentResult.PaymentId}");
                return;
            }

            // Create PaymentTransaction record with the details received from VNPay
            var paymentTransaction = new PaymentTransaction
            {
                PaymentId = payment.Id,
                TransactionId = paymentResult.VnpayTransactionId.ToString(),
                ResponseCode = paymentResult.PaymentResponse.Code.ToString(),
                CreatedAt = paymentResult.Timestamp
            };

            // Save PaymentTransaction record
            await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);

            // Update payment status to 'Paid' if the transaction is successful
            if (paymentResult.PaymentResponse.Code == ResponseCode.Code_00)
            {
                payment.PaymentStatus = PaymentStatus.Success;
                await _unitOfWork.PaymentRepository.Update(payment);
            }

            // Commit changes to the database
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Lỗi trong xử lý IPN: {ex.Message}");
        }
    }
}