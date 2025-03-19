using Microsoft.AspNetCore.Http;
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
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVnPayService _vnPayService;

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
            _logger.Info($"Initiating payment URL generation for Appointment ID: {appointmentId}");

            if (_vnPayService == null)
            {
                _logger.Error("VNPay service is not initialized.");
                throw new InvalidOperationException("VNPay service is not initialized.");
            }

            var appointmentVaccine = await _unitOfWork.AppointmentsVaccineRepository
                .FirstOrDefaultAsync(av => av.AppointmentId == appointmentId);

            if (appointmentVaccine == null)
            {
                _logger.Warn($"No appointment vaccine found for Appointment ID: {appointmentId}");
                return null;
            }

            var totalAmount = appointmentVaccine.TotalPrice ?? 0m;
            _logger.Info($"Total amount for appointment {appointmentId}: {totalAmount} VND");

            string generatedOrderId;
            var paymentInfo = new PaymentInformationModel
            {
                Amount = (long)(totalAmount * 100),
                OrderType = "other",
                OrderDescription = $"Payment for vaccination - {totalAmount} VND",
                Name = "VaccinePayment",
                OrderId = "",
                PaymentCallbackUrl = "http://localhost:5000/api/payments"
            };

            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, context, out generatedOrderId);
            _logger.Info($"Generated VNPay order ID: {generatedOrderId}");

            var existingPayment = await _unitOfWork.PaymentRepository
                .FirstOrDefaultAsync(p => p.OrderId == generatedOrderId);

            if (existingPayment == null)
            {
                var newPayment = new Payment
                {
                    AppointmentId = appointmentId,
                    OrderId = generatedOrderId,
                    OrderDescription = $"Payment for vaccination - {totalAmount} VND",
                    PaymentMethod = "VnPay",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(newPayment);
                await _unitOfWork.SaveChangesAsync();
                _logger.Success($"Payment record created successfully for Order ID: {generatedOrderId}");
            }
            else
            {
                _logger.Warn($"Payment record already exists for Order ID: {generatedOrderId}");
            }

            return paymentUrl;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating payment URL: {ex.Message}");
            throw;
        }
    }

    public async Task<PaymentResponseModel> ProcessPaymentCallback(IQueryCollection query)
    {
        try
        {
            _logger.Info("Processing VNPay payment callback.");

            // Giải mã dữ liệu trả về từ VNPay (bao gồm các tham số trong query string)
            var response = _vnPayService.PaymentExecute(query);

            // Kiểm tra tính hợp lệ của phản hồi từ VNPay
            if (response == null || string.IsNullOrEmpty(response.OrderId))
            {
                _logger.Error("Invalid VNPay response received.");
                throw new Exception("Invalid VNPay response.");
            }

            // Tìm payment record từ OrderId trong database
            var payment = await _unitOfWork.PaymentRepository
                .FirstOrDefaultAsync(p => p.OrderId == response.OrderId);

            // Nếu không tìm thấy payment record, báo lỗi
            if (payment == null)
            {
                _logger.Error($"Payment record not found for Order ID: {response.OrderId}");
                throw new Exception("Payment record not found.");
            }

            // Tạo một PaymentTransaction mới và ghi nhận thông tin thanh toán vào database
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

            // Thêm transaction vào database
            await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);
            _logger.Info(
                $"Payment transaction recorded for Order ID: {response.OrderId}, Status: {paymentTransaction.Status}");

            // Nếu thanh toán thành công, cập nhật thông tin của Payment và Appointment
            if (response.Success)
            {
                payment.TransactionId = response.TransactionId;
                payment.VnpayPaymentId = response.PaymentId;

                var appointment = await _unitOfWork.AppointmentRepository
                    .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

                // Nếu có Appointment, cập nhật trạng thái thành Confirmed
                if (appointment != null)
                {
                    appointment.Status = AppointmentStatus.Confirmed;
                    await _unitOfWork.AppointmentRepository.Update(appointment);
                    _logger.Success($"Appointment {appointment.Id} confirmed after successful payment.");
                }
            }

            // Lưu tất cả thay đổi vào database
            await _unitOfWork.SaveChangesAsync();

            return response;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing VNPay payment callback: {ex.Message}");
            throw;
        }
    }
}