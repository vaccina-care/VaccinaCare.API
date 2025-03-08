using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Domain;
using VaccinaCare.Domain.DTOs.PaymentDTOs;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly VaccinaCareDbContext _dbContext;
        private readonly ILoggerService _logger;
        private readonly IEmailService _emailService;
        private readonly IVnPayService _vnPayService;

        public PaymentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IEmailService emailService, IVnPayService vnPayService, VaccinaCareDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = loggerService;
            _emailService = emailService;
            _vnPayService = vnPayService;
            _dbContext = dbContext;
        }

        public async Task<string> GetPaymentUrl(Guid appointmentId, HttpContext context)
        {
            try
            {
                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized.");
                }

                if (_vnPayService == null)
                {
                    throw new InvalidOperationException("VNPay service is not initialized.");
                }

                var appointment = await _dbContext.Appointments
                                                  .Include(a => a.AppointmentsVaccines)
                                                  .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    _logger?.Error("Appointment not found.");  // Using null-conditional operator to avoid null reference on logger
                    return null;
                }

                if (appointment.AppointmentsVaccines == null || !appointment.AppointmentsVaccines.Any())
                {
                    _logger?.Error("No vaccine details found for the appointment.");
                    return null;
                }

                double totalAmount = appointment.AppointmentsVaccines.Sum(av => (double)(av.TotalPrice ?? 0));

                var paymentInfo = new PaymentInformationModel
                {
                    Amount = totalAmount,
                    OrderType = "other",
                    OrderDescription = "Payment for vaccination",
                    Name = "Vaccine Payment"
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, context);
                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to create payment URL: {ex.Message}");  // Using null-conditional operator to avoid null reference on logger
                throw;  // Re-throwing to maintain stack trace
            }
        }




    }
}
