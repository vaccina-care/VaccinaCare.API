using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Interface.PaymentService;
using VaccinaCare.Domain;
using VaccinaCare.Domain.DTOs.PaymentDTOs;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    private readonly IEmailService _emailService;
    private readonly IVnPayService _vnPayService;

    public PaymentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IEmailService emailService,
        IVnPayService vnPayService, VaccinaCareDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _emailService = emailService;
        _vnPayService = vnPayService;
    }

    public async Task<string> GetPaymentUrl(Guid appointmentId, HttpContext context)
    {
        try
        {
            if (_vnPayService == null) throw new InvalidOperationException("VNPay service is not initialized.");

            // Lấy danh sách các vaccine của cuộc hẹn dựa trên appointmentId
            var appointmentVaccine = await _unitOfWork.AppointmentsVaccineRepository
                .FirstOrDefaultAsync(av => av.AppointmentId == appointmentId);

            if (appointmentVaccine == null) return null;

            var totalAmount = appointmentVaccine.TotalPrice ?? 0m;

            var paymentInfo = new PaymentInformationModel
            {
                Amount = (long)(totalAmount * 100),
                OrderType = "other",
                OrderDescription = "Payment for vaccination",
                Name = "VaccinePayment"
            };

            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, context);

            return paymentUrl;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}