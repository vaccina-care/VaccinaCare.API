using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        _vnPay.Initialize(_configuration["Payment:VnPay:TmnCode"], _configuration["Payment:VnPay:HashSecret"],
            _configuration["Payment:VnPay:BaseUrl"], _configuration["Payment:VnPay:CallbackUrl"]);
    }

// Dictionary ánh xạ `long` PaymentId của VNPay → `Guid` của hệ thống
    private static readonly ConcurrentDictionary<long, Guid> _paymentMapping = new();

    public async Task<string> CreatePaymentUrl(Guid appointmentId)
    {
        try
        {
            var appointment = await _unitOfWork.AppointmentRepository
                .FirstOrDefaultAsync(a => a.Id == appointmentId, a => a.AppointmentsVaccines);

            if (appointment == null)
                throw new ArgumentException("Lịch hẹn không tồn tại.");

            if (appointment.AppointmentsVaccines == null || !appointment.AppointmentsVaccines.Any())
                throw new ArgumentException("Lịch hẹn này không có mũi tiêm nào để thanh toán.");

            var totalDeposit = appointment.AppointmentsVaccines.Sum(av => av.TotalPrice ?? 0) * 0.2m;

            if (totalDeposit <= 0)
                throw new ArgumentException("Số tiền thanh toán không hợp lệ.");

            var ipAddress = _claimsService.IpAddress ?? "127.0.0.1";

            // 🔹 Tạo `PaymentId` dạng `long` dựa trên timestamp để gửi đến VNPay
            var vnpayPaymentId = DateTime.UtcNow.Ticks;

            // 🔹 Tạo `Payment` trong hệ thống với `Guid`
            var payment = new Payment
            {
                Id = Guid.NewGuid(), // Payment ID là Guid
                AppointmentId = appointmentId,
                Amount = totalDeposit,
                PaymentStatus = PaymentStatus.Pending
            };

            // Ánh xạ `long` ID của VNPay → `Guid` của hệ thống
            _paymentMapping[vnpayPaymentId] = payment.Id;

            await _unitOfWork.PaymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            // 🔹 Tạo request thanh toán đến VNPay
            var request = new PaymentRequest
            {
                PaymentId = vnpayPaymentId, // Sử dụng `long` ID gửi đến VNPay
                Money = (double)totalDeposit,
                Description = $"Thanh toán cọc 20% cho lịch hẹn {appointment.Id}",
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            var paymentUrl = _vnPay.GetPaymentUrl(request);

            _loggerService.Info(
                $"Tạo thanh toán thành công cho Appointment {appointmentId}, PaymentId: {vnpayPaymentId}, Số tiền: {totalDeposit} VND.");

            return paymentUrl;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Lỗi khi tạo thanh toán: {e.Message}");
            throw;
        }
    }


    public async Task<PaymentResult> IpnAction(IQueryCollection parameters)
    {
        try
        {
            _loggerService.Info("Nhận phản hồi từ VNPay...");

            // 🔹 Nhận và xử lý phản hồi từ VNPay
            var paymentResult = _vnPay.GetPaymentResult(parameters);

            _loggerService.Info($"VNPay Response: {JsonConvert.SerializeObject(paymentResult)}");

            // 🔹 Lấy `Guid` của Payment từ Dictionary
            if (!_paymentMapping.TryGetValue(paymentResult.PaymentId, out var paymentGuid))
            {
                _loggerService.Error($"Không tìm thấy ánh xạ `PaymentId` {paymentResult.PaymentId} từ VNPay.");
                throw new ArgumentException("Không tìm thấy Payment.");
            }

            // 🔹 Xác định `Payment` trong hệ thống
            var payment = await _unitOfWork.PaymentRepository.FirstOrDefaultAsync(p => p.Id == paymentGuid);

            if (payment == null)
            {
                _loggerService.Error($"Không tìm thấy Payment với ID {paymentGuid}.");
                throw new ArgumentException("Thanh toán không hợp lệ.");
            }

            // 🔹 Xác định Appointment dựa trên Payment
            var appointment =
                await _unitOfWork.AppointmentRepository.FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

            if (appointment == null)
            {
                _loggerService.Error($"Không tìm thấy Appointment liên kết với Payment {paymentGuid}.");
                throw new ArgumentException("Lịch hẹn không hợp lệ.");
            }

            // 🔹 Cập nhật trạng thái Payment và Appointment dựa trên kết quả VNPay
            if (paymentResult.IsSuccess)
            {
                _loggerService.Success(
                    $"Thanh toán thành công cho Appointment {appointment.Id} với số tiền {payment.Amount} VND.");

                payment.PaymentStatus = PaymentStatus.Success;
                appointment.Status = AppointmentStatus.Confirmed;
            }
            else
            {
                _loggerService.Warn($"Thanh toán thất bại cho Appointment {appointment.Id}. Hủy giao dịch.");

                payment.PaymentStatus = PaymentStatus.Failed;
                appointment.Status = AppointmentStatus.Pending; // Hoặc có thể để Cancelled tùy vào chính sách
            }

            // 🔹 Lưu thay đổi vào database
            await _unitOfWork.PaymentRepository.Update(payment);
            await _unitOfWork.AppointmentRepository.Update(appointment);
            await _unitOfWork.SaveChangesAsync();

            // 🔹 Xóa ánh xạ `PaymentId` sau khi xử lý xong để tránh lưu trữ lâu dài
            _paymentMapping.TryRemove(paymentResult.PaymentId, out _);

            return paymentResult;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Lỗi khi xử lý phản hồi VNPay: {e.Message}");
            throw;
        }
    }
}