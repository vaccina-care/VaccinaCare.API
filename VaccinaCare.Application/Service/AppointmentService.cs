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

        public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IVaccineService vaccineService)
        {
            _unitOfWork = unitOfWork;
            _logger = loggerService;
            _claimsService = claimsService;
            _vaccineService = vaccineService;
        }

        /// <summary>
        /// Tự động tạo danh sách lịch hẹn dựa trên các vaccine được chọn.
        /// </summary>
        /// <param name="childId"></param>
        /// <param name="selectedVaccineIds"></param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Appointment>> GenerateAppointmentsForVaccines(Guid childId, List<Guid> selectedVaccineIds, DateTime startDate)
        {
            var appointments = new List<Appointment>();
            var vaccineSchedule = new Dictionary<Guid, DateTime>();

            foreach (var vaccineId in selectedVaccineIds)
            {
                DateTime appointmentDate = startDate;

                foreach (var scheduledVaccine in vaccineSchedule)
                {
                    var minDays = await _vaccineService.GetMinIntervalDays(vaccineId, scheduledVaccine.Key);
                    if (minDays > 0)
                    {
                        var possibleDate = scheduledVaccine.Value.AddDays(minDays);
                        if (possibleDate > appointmentDate)
                        {
                            appointmentDate = possibleDate;
                        }
                    }
                }

                var appointment = new Appointment
                {
                    ChildId = childId,
                    AppointmentDate = appointmentDate,
                    Status = AppointmentStatus.Pending,
                    VaccineType = VaccineType.SingleDose,
                    AppointmentsVaccines = new List<AppointmentsVaccine>
            {
                new AppointmentsVaccine
                {
                    VaccineId = vaccineId,
                    DoseNumber = 1,
                    TotalPrice = await _vaccineService.GetVaccinePrice(vaccineId)
                }
            }
                };

                appointments.Add(appointment);
                vaccineSchedule[vaccineId] = appointmentDate;
            }

            return appointments;
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
                    PolicyId = createAppointmentDto.PolicyId,
                    Notes = createAppointmentDto.Notes,
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

        public Task<CreateAppointmentDto> UpdateAppointment(Guid id, CreateAppointmentDto createAppointmentDto)
        {
            throw new NotImplementedException();
        }
    }
}