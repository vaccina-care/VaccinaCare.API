using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;
        private readonly IClaimsService _claimsService;

        public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _logger = loggerService;
            _claimsService = claimsService;
        }

        public async Task<CreateAppointmentDto> CreateAppointment(CreateAppointmentDto createAppointmentDto)
        {
            _logger.Info("Starting to create a new appointment.");
            try
            {
                if (createAppointmentDto == null)
                {
                    _logger.Error("Appointment creation failed: Input data is null.");
                    throw new ArgumentNullException(nameof(createAppointmentDto));
                }

                if (createAppointmentDto.AppointmentDate < DateTime.UtcNow)
                {
                    _logger.Error("Appointment creation failed: Date is in the past.");
                    throw new ArgumentException("Appointment date must be in the future.");
                }

                var appointment = new Appointment
                {
                    ParentId = createAppointmentDto.ParentId,
                    ChildId = createAppointmentDto.ChildId,
                    AppointmentDate = createAppointmentDto.AppointmentDate,
                    VaccineType = createAppointmentDto.VaccineType,
                    TotalPrice = createAppointmentDto.TotalPrice,
                    PolicyId = createAppointmentDto.PolicyId,
                    Notes = createAppointmentDto.Notes,
                    Confirmed = createAppointmentDto.Confirmed,
                    AppointmentsVaccines = new List<AppointmentsVaccine>()
                };
                foreach (var vaccineId in createAppointmentDto.AppointmentsVaccines)
                {
                    appointment.AppointmentsVaccines.Add(new AppointmentsVaccine
                    {
                        AppointmentId = appointment.Id,
                        VaccineId = vaccineId
                    });
                }

                await _unitOfWork.AppointmentRepository.AddAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                _logger.Info($"Appointment {appointment.Id} created successfully.");
                return createAppointmentDto;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred while creating the appointment. Error: {ex.Message}");
                throw;
            }
        }

        public Task<CreateAppointmentDto> DeleteAppointment(Guid id)
        {
            throw new NotImplementedException();
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
                    TotalPrice = appointment.TotalPrice ?? 0m,
                    PolicyId = appointment.PolicyId ?? Guid.Empty,
                    Notes = appointment.Notes,
                    Confirmed = appointment.Confirmed ?? false,
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

        public Task<CreateAppointmentDto> UpdateAppointment(Guid id, CreateAppointmentDto createAppointmentDto)
        {
            throw new NotImplementedException();
        }
    }
}