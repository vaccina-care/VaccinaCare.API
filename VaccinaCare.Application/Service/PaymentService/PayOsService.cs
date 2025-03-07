using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using System.Data.Entity;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using EntityFrameworkQueryableExtensions = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions;

namespace VaccinaCare.Application.Service.PaymentService;

public class PayOsService : IPayOsService
{
    private readonly PayOS _payOS;
    private readonly ILoggerService _logger;
    private readonly VaccinaCareDbContext _context;

    public PayOsService(PayOS payOS, ILoggerService logger, VaccinaCareDbContext dbContext)
    {
        _payOS = payOS;
        _logger = logger;
        _context = dbContext;
    }

    public async Task<string> ProcessPayment(Guid appointmentId)
    {
        try
        {
            // 1. Lấy thông tin AppointmentsVaccine thông qua AppointmentId
            _logger.Info($"Bắt đầu xử lý thanh toán cho AppointmentId: {appointmentId}");

            var appointmentVaccine = await _context.AppointmentsVaccines
                .Include(x => x.Vaccine) // include thêm thông tin vaccine nếu muốn
                .FirstOrDefaultAsync(av => av.AppointmentId == appointmentId);

            if (appointmentVaccine == null)
            {
                _logger.Error($"Không tìm thấy thông tin vaccine cho AppointmentId: {appointmentId}");
                throw new Exception("Appointment vaccine not found");
            }

            // 2. Kiểm tra giá tiền thanh toán (TotalPrice) và đảm bảo không phải null
            var priceToPay = appointmentVaccine.TotalPrice ?? 0; // Gán giá trị mặc định là 0 nếu TotalPrice là null

            // Log giá thanh toán
            _logger.Info($"Giá thanh toán cho vaccine {appointmentVaccine.Vaccine.VaccineName} là: {priceToPay}");

            // 3. Tạo Payment trước khi tạo link thanh toán
            var payment = new Payment
            {
                AppointmentId = appointmentId,
                Amount = priceToPay,
                PaymentStatus = PaymentStatus.Pending,
                PaymentType = PaymentType.Deposit, // hoặc PaymentType.Full theo nghiệp vụ
                PaymentDate = DateTime.Now,
            };

            _logger.Info($"Tạo payment với Amount: {priceToPay}, Status: {PaymentStatus.Pending}");

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            _logger.Info($"Đã tạo Payment với ID: {payment.Id} cho AppointmentId: {appointmentId}");

            // 4. Tạo thông tin ItemData gửi lên PayOS
            var itemList = new List<ItemData>()
            {
                new(
                    $"Thanh toán vaccine {appointmentVaccine.Vaccine.VaccineName}",
                    1,
                    (int)priceToPay
                )
            };

            _logger.Info(
                $"Tạo ItemData cho PayOS: Tên vaccine {appointmentVaccine.Vaccine.VaccineName}, Số lượng: 1, Giá: {priceToPay}");

            // 5. Tạo paymentData đúng theo cấu trúc yêu cầu từ PayOS
            var paymentData = new PaymentData(
                BitConverter.ToInt64(payment.Id.ToByteArray(), 0), // chuyển Guid -> long cho PayOS
                (int)priceToPay,
                $"Thanh toán vaccine {appointmentVaccine.Vaccine.VaccineName}",
                itemList,
                returnUrl: "https://vaccina-care-fe.vercel.app",
                cancelUrl: "https://ae-tao-fullstack-api.site/index.html"
            );

            _logger.Info(
                $"Tạo PaymentData cho PayOS: OrderCode = {payment.Id}, Amount = {priceToPay}, Description = {paymentData.description}");

            // 6. Gọi API của PayOS để tạo link thanh toán
            var paymentResult = await _payOS.createPaymentLink(paymentData);
            if (paymentResult == null || string.IsNullOrEmpty(paymentResult.checkoutUrl))
            {
                _logger.Error($"Không thể tạo link thanh toán cho PaymentId: {payment.Id}");
                throw new Exception("Không thể tạo link thanh toán!");
            }

            _logger.Info(
                $"Link thanh toán đã được tạo thành công cho PaymentId: {payment.Id}, URL: {paymentResult.checkoutUrl}");

            // Trả về checkout URL
            return paymentResult.checkoutUrl;
        }
        catch (Exception e)
        {
            // Log lỗi nếu có
            _logger.Error($"Lỗi khi xử lý thanh toán cho AppointmentId: {appointmentId}. Lỗi: {e.Message}");
            Console.WriteLine(e); // Bạn có thể ghi log thêm ở đây nếu cần
            throw;
        }
    }


    public async Task<IActionResult> PaymentWebhook([FromBody] WebhookData webhookData)
    {
        try
        {
            _logger.Info($"Nhận webhook từ PayOS: {JsonConvert.SerializeObject(webhookData)}");

            // 1. Lấy thông tin đơn hàng từ webhook
            var orderCode = webhookData.orderCode; // ID của Payment trong hệ thống
            var statusCode = webhookData.code; // Mã trạng thái giao dịch từ PayOS
            var transactionId = webhookData.reference; // Mã giao dịch của PayOS
            var transactionAmount = webhookData.amount; // Số tiền thanh toán thực tế

            // 2. Tìm Payment tương ứng trong database
            var orderCodeGuid = new Guid(orderCode.ToString()); // Chuyển đổi orderCode từ long sang Guid

            var payment = await _context.Payments
                .Include(p => p.PaymentTransactions)
                .FirstOrDefaultAsync(p => p.Id == orderCodeGuid);

            if (payment == null)
            {
                _logger.Warn($"Không tìm thấy Payment với orderCode: {orderCode}");
                return new NotFoundObjectResult("Payment not found");
            }

            // 4. Xử lý trạng thái thanh toán dựa trên statusCode từ PayOS
            PaymentStatus updatedStatus;
            PaymentTransactionStatus transactionStatus;

            switch (statusCode)
            {
                case "00": // Thanh toán thành công
                    updatedStatus = PaymentStatus.Success;
                    transactionStatus = PaymentTransactionStatus.Success;
                    break;

                case "01": // Thanh toán thất bại
                case "02": // Người dùng hủy thanh toán
                    updatedStatus = PaymentStatus.Cancelled;
                    transactionStatus = PaymentTransactionStatus.Failed;
                    break;

                default:
                    _logger.Warn($"Trạng thái không xác định từ PayOS: {statusCode}");
                    return new BadRequestObjectResult("Unknown payment status");
            }

            // 5. Cập nhật Payment status
            payment.PaymentStatus = updatedStatus;

            // 6. Tạo bản ghi PaymentTransaction
            var paymentTransaction = new PaymentTransaction
            {
                PaymentId = payment.Id,
                Amount = transactionAmount,
                TransactionId = transactionId,
                TransactionDate = DateTime.UtcNow,
                ResponseCode = statusCode,
                ResponseMessage = webhookData.description,
                Status = transactionStatus,
                Note = "Xử lý từ webhook PayOS"
            };

            await _context.PaymentTransactions.AddAsync(paymentTransaction);

            // 7. Nếu thanh toán thành công, tạo Invoice
            if (updatedStatus == PaymentStatus.Success)
            {
                var invoice = new Invoice
                {
                    PaymentId = payment.Id,
                    TotalAmount = payment.Amount
                };

                await _context.Invoices.AddAsync(invoice);
            }

            // 8. Lưu thay đổi vào database
            await _context.SaveChangesAsync();

            _logger.Info($"Webhook xử lý thành công cho PaymentId: {payment.Id}");
            return new OkObjectResult(new { success = true, message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi xử lý webhook từ PayOS: {ex.Message}");
            return new StatusCodeResult(500);
        }
    }
}