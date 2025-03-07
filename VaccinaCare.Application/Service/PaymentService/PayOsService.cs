using Net.payOS;
using Net.payOS.Types;
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

        public async Task<string> ProcessPayment(Guid appointmentid)
        {
            // 1. Kiểm tra và lấy thông tin Appointment và giá
            var appointmentVaccine = await _context.AppointmentsServices
                .FirstOrDefaultAsync(av => av.AppointmentId == appointmentid);

            if (appointmentVaccine == null)
                throw new Exception("Appointment vaccine not found");


            decimal priceToPay = appointmentVaccine.TotalPrice ?? 0;

            // 2. Tạo Payment trước khi tạo link thanh toán
            var payment = new Payment
            {
                AppointmentId = appointmentid,
                Amount = priceToPay,
                PaymentStatus = PaymentStatus.Pending,
                PaymentType = PaymentType.Deposit,
                PaymentDate = null, // vì chưa thanh toán thành công
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            // 5. Gọi PayOS API để tạo link thanh toán
            var paymentData = new PaymentData(
            payment.Id,
                (int)cart.TotalPrice,
                "image payment",
                itemList,
                returnUrl: "https://arwoh-fe.vercel.app/",
                cancelUrl: "https://arwoh.ae-tao-fullstack-api.site/"
            );
        }


    }
}
