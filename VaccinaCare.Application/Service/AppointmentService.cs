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


        public async Task<Pagination<CreateAppointmentDto>> GetAppointmentByParent(Guid parentId,
            PaginationParameter pagination)
        {
            try
            {
                // Guid parentId = _claimsService.GetCurrentUserId;

                _logger.Info(
                    $"Fetching appointments for parent {parentId} with pagination: Page {pagination.PageIndex}, Size {pagination.PageSize}");

                var query = _unitOfWork.AppointmentRepository.GetQueryable()
                    .Where(a => a.ParentId == parentId);

                int totalAppointments = await query.CountAsync();

                var appointments = await query
                    .OrderByDescending(a => a.AppointmentDate)
                    .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();
                if (!appointments.Any())
                {
                    _logger.Warn($"No appointments found for parent {parentId} on page {pagination.PageIndex}.");
                    return new Pagination<CreateAppointmentDto>(new List<CreateAppointmentDto>(), 0,
                        pagination.PageIndex, pagination.PageSize);
                }

                _logger.Success(
                    $"Retrieved {appointments.Count} appointments for parent {parentId} on page {pagination.PageIndex}");

                var appointmentDtos = appointments.Select(appointment => new CreateAppointmentDto
                {
                    ParentId = appointment.ParentId ?? Guid.Empty,
                    ChildId = appointment.ChildId ?? Guid.Empty,
                    AppointmentDate = appointment.AppointmentDate ?? DateTime.UtcNow,
                    VaccineType = appointment.VaccineType,
                    PolicyId = appointment.PolicyId ?? Guid.Empty,
                    Notes = appointment.Notes,
                    AppointmentsVaccines = appointment.AppointmentsVaccines
                        .Where(v => v.VaccineId.HasValue)
                        .Select(v => v.VaccineId.Value)
                        .ToList(),
                }).ToList();
                return new Pagination<CreateAppointmentDto>(appointmentDtos, totalAppointments, pagination.PageIndex,
                    pagination.PageSize);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error while fetching appointments: {ex.Message}");
                throw new Exception("An error occurred while fetching appointments. Please try again later.");
            }
        }
    }
}