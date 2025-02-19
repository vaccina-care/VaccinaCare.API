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

        public async Task<IEnumerable<Appointment>> GenerateAppointmentsFromVaccineSuggestions(Guid childId, DateTime startDate)
        {
            try
            {
                _logger.Info($"Generating appointments from vaccine suggestions for child ID: {childId}");

                // Lấy danh sách vaccine đã tư vấn từ VaccineSuggestion
                var vaccineSuggestions = await _unitOfWork.VaccineSuggestionRepository
                    .GetAllAsync(vs => vs.ChildId == childId);

                if (vaccineSuggestions == null || !vaccineSuggestions.Any())
                {
                    _logger.Warn($"No vaccine suggestions found for child ID: {childId}");
                    throw new Exception("No vaccine suggestions found.");
                }

                var selectedVaccineIds = vaccineSuggestions.Select(vs => vs.VaccineId.Value).ToList();

                // Gọi hàm GenerateAppointmentsForVaccines để tạo danh sách lịch hẹn từ danh sách vaccine đã tư vấn
                var generatedAppointments = await GenerateAppointmentsForVaccines(childId, selectedVaccineIds, startDate);

                if (!generatedAppointments.Any())
                {
                    _logger.Warn($"No appointments could be generated for child ID: {childId}");
                    throw new Exception("No appointments could be generated.");
                }

                // Lưu danh sách lịch hẹn vào database
                await _unitOfWork.AppointmentRepository.AddRangeAsync(generatedAppointments.ToList());
                await _unitOfWork.SaveChangesAsync();

                _logger.Success($"Generated {generatedAppointments.Count()} appointments for child ID: {childId}");

                return generatedAppointments;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating appointments for child ID {childId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Tạo một lịch hẹn tư vấn cho người dùng mà không cần chọn vaccine trước.
        /// </summary>
        /// <param name="childId"></param>
        /// <param name="appointmentDate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AppointmentDTO> BookConsultationAppointment(Guid childId, DateTime appointmentDate)
        {
            // Check if child exists
            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (child == null)
            {
                throw new Exception("Child not found.");
            }

            var parentId = _claimsService.GetCurrentUserId;

            // Create a new appointment entity
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                ParentId = parentId,
                AppointmentDate = appointmentDate,
                Status = AppointmentStatus.Pending,
                VaccineType = VaccineType.Consultation,
                CreatedAt = DateTime.UtcNow
            };

            // Save the appointment to the database
            await _unitOfWork.AppointmentRepository.AddAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            // Log the operation
            _logger.Info($"Consultation appointment booked with ID: {appointment.Id}");

            // Return the appointment DTO
            return new AppointmentDTO
            {
                Id = appointment.Id,
                ChildId = appointment.ChildId,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                VaccineType = appointment.VaccineType
            };
        }

        /// <summary>
        /// Tự động tạo danh sách lịch hẹn dựa trên các vaccine được chọn.
        /// </summary>
        /// <param name="childId"></param>
        /// <param name="selectedVaccineIds"></param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Appointment>> GenerateAppointmentsForVaccines(Guid childId,
            List<Guid> selectedVaccineIds, DateTime startDate)
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