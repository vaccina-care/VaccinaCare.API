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
    private readonly INotificationService _notificationService;

    public PaymentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IEmailService emailService,
        IVnPayService vnPayService, VaccinaCareDbContext dbContext, IConfiguration configuration, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _emailService = emailService;
        _vnPayService = vnPayService;
        _configuration = configuration;
        _notificationService = notificationService;
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
            _logger.Info("Processing VNPay payment callback...");

            // Giải mã dữ liệu trả về từ VNPay
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

            if (payment == null)
            {
                _logger.Error($"Payment record not found for Order ID: {response.OrderId}");
                throw new Exception("Payment record not found.");
            }

            // Lấy trạng thái thanh toán từ VNPay
            string vnpResponseCode = response.VnPayResponseCode;
            _logger.Info($"Received VNPay Response Code: {vnpResponseCode} for Order ID: {response.OrderId}");

            // 🚨 Đặt mặc định Status là Failed trước khi xử lý
            var paymentTransaction = new PaymentTransaction
            {
                PaymentId = payment.Id,
                TransactionId = response.TransactionId,
                Amount = decimal.Parse(response.OrderDescription.Split(" ").Last()) / 100,
                TransactionDate = DateTime.UtcNow,
                ResponseCode = vnpResponseCode,
                ResponseMessage = (vnpResponseCode == "00") ? "Success" : "Failed",
                Status = PaymentTransactionStatus.Failed // 🚨 Mặc định là Failed
            };

            // Xử lý trạng thái dựa trên ResponseCode
            switch (vnpResponseCode)
            {
                case "00": // ✅ Thanh toán thành công
                    _logger.Info($"Payment successful for Order ID: {response.OrderId}");
                    paymentTransaction.Status = PaymentTransactionStatus.Success;

                    // Cập nhật Payment
                    payment.TransactionId = response.TransactionId;
                    payment.VnpayPaymentId = response.PaymentId;

                    // Cập nhật trạng thái của Appointment thành "Confirmed"
                    var confirmedAppointment = await _unitOfWork.AppointmentRepository
                        .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

                    if (confirmedAppointment != null)
                    {
                        confirmedAppointment.Status = AppointmentStatus.Confirmed;
                        await _unitOfWork.AppointmentRepository.Update(confirmedAppointment);
                        _logger.Success($"Appointment {confirmedAppointment.Id} confirmed after successful payment.");
                        await _notificationService.PushPaymentSuccessNotification(confirmedAppointment.ParentId, confirmedAppointment.Id);
                    }

                    break;

                case "24": // ❌ Người dùng HỦY thanh toán
                case "09": // ❌ Giao dịch bị hủy
                case "07": // ❌ Giao dịch bị nghi ngờ gian lận
                case "10": // ❌ Giao dịch bị từ chối bởi ngân hàng phát hành
                case "99": // ❌ Người dùng không thực hiện thanh toán
                    _logger.Warn(
                        $"Payment failed for Order ID: {response.OrderId} with Response Code: {vnpResponseCode}");
                    paymentTransaction.Status = PaymentTransactionStatus.Failed;

                    // Cập nhật trạng thái Appointment thành "Cancelled"
                    var cancelledAppointment = await _unitOfWork.AppointmentRepository
                        .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

                    if (cancelledAppointment != null)
                    {
                        cancelledAppointment.Status = AppointmentStatus.Cancelled;
                        await _unitOfWork.AppointmentRepository.Update(cancelledAppointment);
                        _logger.Warn(
                            $"Appointment {cancelledAppointment.Id} has been cancelled due to failed payment.");
                    }

                    break;

                default:
                    _logger.Error($"Unknown response code: {vnpResponseCode} for Order ID: {response.OrderId}");
                    break;
            }

            // 🚨 Ghi log trước khi lưu vào database
            _logger.Info(
                $"Saving PaymentTransaction for Order ID: {response.OrderId} with Status: {paymentTransaction.Status}");

            // Ghi nhận transaction vào database
            await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);

            // Gọi SaveChangesAsync để lưu trạng thái chính xác
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