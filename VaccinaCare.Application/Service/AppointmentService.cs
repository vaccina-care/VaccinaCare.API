using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;
        private readonly IVaccineService _vaccineService;
        private readonly IClaimsService _claimsService;

        public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService,
            IVaccineService vaccineService)
        {
            _unitOfWork = unitOfWork;
            _logger = loggerService;
            _claimsService = claimsService;
            _vaccineService = vaccineService;
        }

       

        /// <summary>
        /// Lấy chi tiết Appointment dựa trên ID của Children
        /// </summary>
        /// <param name="childId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Appointment?> GetAppointmentDetailsByChildIdAsync(Guid childId)
        {
            try
            {
                _logger.Info($"Fetching appointment details for child ID: {childId}");

                var childExists = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
                if (childExists == null)
                {
                    _logger.Warn($"Child with ID {childId} not found.");
                    throw new Exception("Child not found.");
                }

                var appointment = await _unitOfWork.AppointmentRepository
                    .FirstOrDefaultAsync(a => a.ChildId == childId, a => a.AppointmentsVaccines);

                if (appointment == null)
                {
                    _logger.Warn($"No appointment found for child ID: {childId}");
                    return null;
                }

                _logger.Success($"Successfully retrieved appointment ID: {appointment.Id} for child ID: {childId}");

                return appointment;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error retrieving appointment details for child ID {childId}: {ex.Message}");
                throw;
            }
        }
        
    }
}