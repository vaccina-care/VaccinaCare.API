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

            var ipAddress = _claimsService.IpAddress;

            //  Tạo `PaymentId` dạng `long` dựa trên timestamp để gửi đến VNPay
            var vnpayPaymentId = DateTime.UtcNow.Ticks;

            //  insert data vào bảng `Payment` trong hệ thống với `Guid`
            var payment = new Payment
            {
                Id = Guid.NewGuid(), // Payment ID là Guid
                AppointmentId = appointmentId,
                Amount = totalDeposit,
                PaymentStatus = PaymentStatus.Pending,
                PaymentType = PaymentType.Deposit, // Xác định là thanh toán cọc
                PaymentDate = null // Chưa thanh toán
            };

            await _unitOfWork.PaymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            //  Tạo một bản ghi Invoice liên kết với Payment
            var invoice = new Invoice
            {
                UserId = appointment.Child.ParentId, // Lấy ParentId của Child liên kết với User
                PaymentId = payment.Id,
                TotalAmount = totalDeposit
            };

            await _unitOfWork.InvoiceRepository.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            //  Tạo request thanh toán đến VNPay
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

            //  Tạo PaymentTransaction để theo dõi giao dịch
            var paymentTransaction = new PaymentTransaction
            {
                PaymentId = payment.Id,
                TransactionId = vnpayPaymentId.ToString(),
                Amount = totalDeposit,
                TransactionDate = DateTime.UtcNow,
                ResponseCode = "00", // Mã trả về từ VNPay, giả sử ban đầu là thành công
                ResponseMessage = "Giao dịch thành công",
                Status = PaymentTransactionStatus.Pending
            };

            await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);
            await _unitOfWork.SaveChangesAsync();

            //  Lấy URL thanh toán từ VNPay
            var paymentUrl = _vnPay.GetPaymentUrl(request);

            _loggerService.Info(
                $"Tạo thanh toán thành công cho Appointment {appointmentId}, PaymentId: {payment.Id}, Số tiền: {totalDeposit} VND.");

            return paymentUrl;
        }
        catch (Exception e)
        {
            _loggerService.Error($"Lỗi khi tạo thanh toán: {e.Message}");
            throw;
        }
    }
}