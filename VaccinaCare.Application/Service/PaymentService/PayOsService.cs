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
namespace VaccinaCare.Application.Service.PaymentService
{
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
            // 1. Lấy thông tin AppointmentsVaccine thông qua AppointmentId
            var appointmentVaccine = await _context.AppointmentsServices
                .Include(x => x.Vaccine) // include thêm thông tin vaccine nếu muốn
                .FirstOrDefaultAsync(av => av.AppointmentId == appointmentId);

            if (appointmentVaccine == null)
                throw new Exception("Appointment vaccine not found");

            // 2. Lấy giá tiền thanh toán từ AppointmentsVaccine
            decimal priceToPay = appointmentVaccine.TotalPrice ?? 0;

            // 3. Tạo Payment trước khi tạo link thanh toán
            var payment = new Payment
            {
                AppointmentId = appointmentId,
                Amount = priceToPay,
                PaymentStatus = PaymentStatus.Pending,
                PaymentType = PaymentType.Deposit, // hoặc PaymentType.Full theo nghiệp vụ
                PaymentDate = null,
                // nếu cần có thể gán PaymentMethodId tương ứng với PayOS tại đây
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            // 4. Tạo thông tin ItemData gửi lên PayOS
            var itemList = new List<ItemData>()
            {
                new ItemData(
                    name: $"Thanh toán vaccine {appointmentVaccine.Vaccine.VaccineName}",
                    quantity: 1,
                    price: (int)priceToPay
                )
            };

            // 5. Tạo paymentData đúng theo cấu trúc yêu cầu từ PayOS
            var paymentData = new PaymentData(
                orderCode: BitConverter.ToInt64(payment.Id.ToByteArray(), 0), // chuyển Guid -> long cho PayOS
                amount: (int)priceToPay,
                description: $"Thanh toán vaccine {appointmentVaccine.Vaccine.VaccineName}",
                items: itemList,
                returnUrl: "https://vaccina-care-fe.vercel.app",
                cancelUrl: "https://ae-tao-fullstack-api.site/index.html"
            );

            // 6. Gọi API của PayOS để tạo link thanh toán
            var paymentResult = await _payOS.createPaymentLink(paymentData);
            if (paymentResult == null || string.IsNullOrEmpty(paymentResult.checkoutUrl))
                throw new Exception("Không thể tạo link thanh toán!");

            return paymentResult.checkoutUrl;
        }

        /// <summary>
        /// Xử lý Webhook từ PayOS
        /// </summary>
        [HttpPost]
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
                var payment = await _context.Payments
                    .Include(p => p.PaymentTransactions)
                    .FirstOrDefaultAsync(p => BitConverter.ToInt64(p.Id.ToByteArray(), 0) == orderCode);

                if (payment == null)
                {
                    _logger.Warn($"Không tìm thấy Payment với orderCode: {orderCode}");
                    return new NotFoundObjectResult("Payment not found");
                }

                // 3. Kiểm tra xem giao dịch đã tồn tại chưa để tránh duplicate
                if (payment.PaymentTransactions.Any(pt => pt.TransactionId == transactionId))
                {
                    _logger.Warn($"Giao dịch đã được xử lý trước đó: {transactionId}");
                    return new OkObjectResult(new { success = true, message = "Transaction already processed" });
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
                        TotalAmount = payment.Amount,
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
}
