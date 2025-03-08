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

            // Tạo Payment nếu chưa có
            var existingPayment = await _unitOfWork.PaymentRepository
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

            if (existingPayment == null)
            {
                existingPayment = new Payment
                {
                    AppointmentId = appointmentId,
                    OrderId = Guid.NewGuid().ToString(),
                    OrderDescription = $"Payment for vaccination - {totalAmount} VND",
                    PaymentMethod = "VnPay",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(existingPayment);
                await _unitOfWork.SaveChangesAsync();
            }

            // Chỉ cần callback về backend, không cần return về frontend
            var paymentInfo = new PaymentInformationModel
            {
                Amount = (long)(totalAmount * 100),
                OrderType = "other",
                OrderDescription = $"Payment for vaccination - {totalAmount} VND",
                Name = "VaccinePayment",
                OrderId = existingPayment.OrderId,
                PaymentCallbackUrl = "http://localhost:5000/api/payments"
            };

            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, context);
            return paymentUrl;
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

        // Tìm Payment dựa trên OrderId
        var payment = await _unitOfWork.PaymentRepository
            .FirstOrDefaultAsync(p => p.OrderId == response.OrderId);

        if (payment == null)
        {
            throw new Exception("Payment record not found.");
        }

        // Lưu giao dịch vào PaymentTransaction
        var paymentTransaction = new PaymentTransaction
        {
            PaymentId = payment.Id,
            TransactionId = response.TransactionId,
            Amount = decimal.Parse(response.OrderDescription.Split(" ").Last()) / 100, // Convert VNPay amount
            TransactionDate = DateTime.Now,
            ResponseCode = response.VnPayResponseCode,
            ResponseMessage = response.Success ? "Success" : "Failed",
            Status = response.Success ? PaymentTransactionStatus.Success : PaymentTransactionStatus.Failed
        };

        await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);

        if (response.Success)
        {
            // Cập nhật Payment
            payment.TransactionId = response.TransactionId;
            payment.VnpayPaymentId = response.PaymentId;

            // **Lấy danh sách các Appointment liên kết với Payment**
            var appointments = await _unitOfWork.AppointmentRepository
                .GetAllAsync(a => a.Id == payment.AppointmentId);

            foreach (var appointment in appointments)
            {
                appointment.Status = AppointmentStatus.Confirmed;
                await _unitOfWork.AppointmentRepository.Update(appointment);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return response;
    }
}