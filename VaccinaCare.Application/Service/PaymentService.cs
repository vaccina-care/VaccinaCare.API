using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Domain;
using VaccinaCare.Domain.DTOs.PaymentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    private readonly IEmailService _emailService;
    private readonly IVnPayService _vnPayService;
    private readonly IConfiguration _configuration;

    public PaymentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IEmailService emailService,
        IVnPayService vnPayService, VaccinaCareDbContext dbContext, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _emailService = emailService;
        _vnPayService = vnPayService;
        _configuration = configuration;
    }

    public async Task<string> GetPaymentUrl(Guid appointmentId, HttpContext context)
    {
        try
        {
            if (_vnPayService == null) throw new InvalidOperationException("VNPay service is not initialized.");

            var appointmentVaccine = await _unitOfWork.AppointmentsVaccineRepository
                .FirstOrDefaultAsync(av => av.AppointmentId == appointmentId);

            if (appointmentVaccine == null) return null;

            var totalAmount = appointmentVaccine.TotalPrice ?? 0m;

            // ✅ Gọi VNPay trước để lấy OrderId (vnp_TxnRef)
            string generatedOrderId;
            var paymentInfo = new PaymentInformationModel
            {
                Amount = (long)(totalAmount * 100),
                OrderType = "other",
                OrderDescription = $"Payment for vaccination - {totalAmount} VND",
                Name = "VaccinePayment",
                OrderId = "", // ✅ Chưa có, sẽ cập nhật sau
                PaymentCallbackUrl = "http://localhost:5000/api/payments"
            };

            var paymentUrl =
                _vnPayService.CreatePaymentUrl(paymentInfo, context,
                    out generatedOrderId); // ✅ Lấy OrderId từ VNPayService

            // ✅ Kiểm tra nếu Payment đã tồn tại
            var existingPayment = await _unitOfWork.PaymentRepository
                .FirstOrDefaultAsync(p => p.OrderId == generatedOrderId);

            if (existingPayment == null)
            {
                // ✅ Lưu Payment vào DB ngay sau khi có OrderId chính xác
                var newPayment = new Payment
                {
                    AppointmentId = appointmentId,
                    OrderId = generatedOrderId, // ✅ Lưu OrderId từ VNPay
                    OrderDescription = $"Payment for vaccination - {totalAmount} VND",
                    PaymentMethod = "VnPay",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(newPayment);
                await _unitOfWork.SaveChangesAsync();
            }

            return paymentUrl; // ✅ Trả về URL thanh toán
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PaymentResponseModel> ProcessPaymentCallback(IQueryCollection query)
    {
        var response = _vnPayService.PaymentExecute(query);

        if (response == null || string.IsNullOrEmpty(response.OrderId))
        {
            throw new Exception("Invalid VNPay response.");
        }

        var payment = await _unitOfWork.PaymentRepository
            .FirstOrDefaultAsync(p => p.OrderId == response.OrderId);

        if (payment == null)
        {
            throw new Exception("Payment record not found.");
        }

        var paymentTransaction = new PaymentTransaction
        {
            PaymentId = payment.Id,
            TransactionId = response.TransactionId,
            Amount = decimal.Parse(response.OrderDescription.Split(" ").Last()) / 100,
            TransactionDate = DateTime.UtcNow,
            ResponseCode = response.VnPayResponseCode,
            ResponseMessage = response.Success ? "Success" : "Failed",
            Status = response.Success ? PaymentTransactionStatus.Success : PaymentTransactionStatus.Failed
        };

        await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);

        if (response.Success)
        {
            payment.TransactionId = response.TransactionId;
            payment.VnpayPaymentId = response.PaymentId;

            var appointment = await _unitOfWork.AppointmentRepository
                .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Confirmed;
                await _unitOfWork.AppointmentRepository.Update(appointment);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return response;
    }
}