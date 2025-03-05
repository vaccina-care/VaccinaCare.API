using LinqKit;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    private readonly IVaccineService _vaccineService;
    private readonly IClaimsService _claimsService;
    private readonly IVaccineRecordService _vaccineRecordService;
    private readonly INotificationService _notificationService;

    public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService,
        IVaccineService vaccineService, IVaccineRecordService vaccineRecordService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _claimsService = claimsService;
        _vaccineService = vaccineService;
        _vaccineRecordService = vaccineRecordService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Tạo danh sách các cuộc hẹn cho một loại vaccine duy nhất, bao gồm tất cả các mũi tiêm cần thiết.
    /// Kiểm tra các điều kiện trước khi đặt lịch, bao gồm:
    /// - Trẻ có đủ điều kiện để tiêm vaccine không.
    /// - Trẻ đã tiêm đủ số mũi chưa.
    /// - Vaccine có gây xung đột với các vaccine đã đặt trước không.
    /// - Ngăn chặn spam đặt cùng một loại vaccine trong khoảng thời gian `DoseIntervalDays`.
    /// Nếu mọi điều kiện hợp lệ, hệ thống sẽ tự động tạo danh sách các cuộc hẹn theo khoảng cách mũi.
    /// </summary>
    public async Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentDto request,
        Guid parentId)
    {
        try
        {
            var appointments = new List<Appointment>();

            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(request.VaccineId);
            if (vaccine == null)
                throw new ArgumentException($"Vaccine với ID {request.VaccineId} không tồn tại.");

            var (isEligible, message) =
                await _vaccineService.CanChildReceiveVaccine(request.ChildId, request.VaccineId);
            if (!isEligible)
                throw new ArgumentException($"Trẻ không đủ điều kiện tiêm vaccine {vaccine.VaccineName}: {message}");

            var nextDose = await _vaccineService.GetNextDoseNumber(request.ChildId, request.VaccineId);
            if (nextDose > vaccine.RequiredDoses)
                throw new ArgumentException($"Trẻ đã tiêm đủ số mũi của vaccine {vaccine.VaccineName}.");

            var appointmentDate = request.StartDate;

            // 🔹 Kiểm tra xem vaccine này đã có lịch hẹn trong khoảng thời gian gần đây chưa
            var recentAppointments = await _unitOfWork.AppointmentRepository
                .GetAllAsync(a => a.ChildId == request.ChildId &&
                                  a.AppointmentsVaccines.Any(av => av.VaccineId == request.VaccineId) &&
                                  a.AppointmentDate >= appointmentDate.AddDays(-vaccine.DoseIntervalDays));

            if (recentAppointments.Any())
                throw new ArgumentException(
                    $"Trẻ đã có lịch hẹn tiêm {vaccine.VaccineName} gần đây. Vui lòng chọn ngày khác.");

            // 🔹 Kiểm tra các vaccine đã đặt trước đó
            var existingAppointments = await _unitOfWork.AppointmentRepository
                .GetAllAsync(a => a.ChildId == request.ChildId && a.AppointmentsVaccines.Any());

            var bookedVaccineIds = existingAppointments
                .SelectMany(a => a.AppointmentsVaccines.Select(av => av.VaccineId))
                .Distinct()
                .ToList();

            // 🔹 Kiểm tra xem vaccine mới có xung đột với các vaccine đã đặt trước không
            if (!await _vaccineService.CheckVaccineCompatibility(request.VaccineId, bookedVaccineIds, appointmentDate))
                throw new ArgumentException(
                    $"Vaccine {vaccine.VaccineName} không thể tiêm cùng các loại vaccine đã đặt trước.");

            for (var dose = nextDose; dose <= vaccine.RequiredDoses; dose++)
            {
                var currentBookedVaccineIds = appointments
                    .SelectMany(a => a.AppointmentsVaccines.Select(av => av.VaccineId)).ToList();

                if (!await _vaccineService.CheckVaccineCompatibility(request.VaccineId, currentBookedVaccineIds,
                        appointmentDate))
                    throw new ArgumentException(
                        $"Vaccine {vaccine.VaccineName} không thể tiêm cùng các loại vaccine đã đặt trước.");

                var appointment = new Appointment
                {
                    ParentId = parentId,
                    ChildId = request.ChildId,
                    AppointmentDate = appointmentDate,
                    Status = AppointmentStatus.Pending,
                    VaccineType = VaccineType.SingleDose,
                    Notes = $"Mũi {dose}/{vaccine.RequiredDoses} của {vaccine.VaccineName}",
                    AppointmentsVaccines = new List<AppointmentsVaccine>
                    {
                        new()
                        {
                            VaccineId = request.VaccineId,
                            DoseNumber = dose,
                            TotalPrice = vaccine.Price
                        }
                    }
                };

                appointments.Add(appointment);
                appointmentDate = appointmentDate.AddDays(vaccine.DoseIntervalDays);
            }

            await _unitOfWork.AppointmentRepository.AddRangeAsync(appointments);
            await _unitOfWork.SaveChangesAsync();

            var appointmentDTOs = appointments.Select(a => new AppointmentDTO
            {
                AppointmentId = a.Id,
                ChildId = a.ChildId.Value,
                AppointmentDate = a.AppointmentDate.Value,
                Status = a.Status.ToString(),
                VaccineName = a.AppointmentsVaccines.First().Vaccine?.VaccineName ?? "Unknown",
                DoseNumber = a.AppointmentsVaccines.First().DoseNumber.Value,
                TotalPrice = a.AppointmentsVaccines.First().TotalPrice.Value,
                Notes = a.Notes
            }).ToList();

            return appointmentDTOs;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error in GenerateAppointments: {ex.Message}");
            throw new Exception("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public async Task<bool> UpdateAppointmentStatusByStaffAsync(Guid appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _unitOfWork.AppointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
            throw new Exception($"Không tìm thấy cuộc hẹn với ID {appointmentId}.");

        // Kiểm tra quyền Staff
        var currentUserId = _claimsService.GetCurrentUserId;
        var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId);
        if (currentUser == null || currentUser.RoleName != RoleType.Staff)
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật trạng thái cuộc hẹn.");

        // Xử lý cập nhật trạng thái
        switch (newStatus)
        {
            case AppointmentStatus.Confirmed:
                if (appointment.Status != AppointmentStatus.Pending)
                    throw new Exception("Chỉ có thể xác nhận cuộc hẹn khi đang ở trạng thái Pending.");
                break;

            case AppointmentStatus.Completed:
                if (appointment.Status != AppointmentStatus.Confirmed)
                    throw new Exception("Chỉ có thể hoàn thành cuộc hẹn khi đã được xác nhận.");

                // Cập nhật vào bảng `VaccinationRecord`
                var vaccineRecords = appointment.AppointmentsVaccines.Select(av => new VaccinationRecord
                {
                    ChildId = appointment.ChildId,
                    VaccineId = av.VaccineId,
                    VaccinationDate = appointment.AppointmentDate,
                    DoseNumber = av.DoseNumber ?? 1
                }).ToList();

                await _unitOfWork.VaccinationRecordRepository.AddRangeAsync(vaccineRecords);
                break;

            case AppointmentStatus.Cancelled:
                if (appointment.Status == AppointmentStatus.Completed)
                    throw new Exception("Không thể hủy cuộc hẹn đã hoàn thành.");
                break;

            default:
                throw new Exception("Trạng thái không hợp lệ.");
        }

        appointment.Status = newStatus;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<List<AppointmentDTO>> GetAllAppointmentsByChildIdAsync(Guid childId)
    {
        try
        {
            _logger.Info($"Fetching appointment details for child ID: {childId}");

            // Kiểm tra nếu trẻ tồn tại
            var childExists = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (childExists == null)
            {
                _logger.Warn($"Child with ID {childId} not found.");
                throw new Exception("Child not found.");
            }

            // Lấy danh sách các cuộc hẹn của trẻ, Include AppointmentsVaccines và Vaccine
            var appointments = await _unitOfWork.AppointmentRepository.GetQueryable()
                .Where(a => a.ChildId == childId)
                .Include(a => a.AppointmentsVaccines) // Bao gồm danh sách vaccine
                .ThenInclude(av => av.Vaccine) // Nạp Vaccine đúng cách
                .ToListAsync(); // Chuyển đổi sang danh sách

            if (!appointments.Any())
            {
                _logger.Info($"No appointments found for child ID: {childId}");
                return new List<AppointmentDTO>();
            }

            var appointmentDTOs = appointments.Select(a => new AppointmentDTO
            {
                AppointmentId = a.Id,
                ChildId = a.ChildId.Value,
                AppointmentDate = a.AppointmentDate.Value,
                Status = a.Status.ToString(),
                VaccineName =
                    a.AppointmentsVaccines.FirstOrDefault()?.Vaccine?.VaccineName ?? "Unknown", // ✅ Fix lỗi VaccineName
                DoseNumber = a.AppointmentsVaccines.FirstOrDefault()?.DoseNumber ?? 0,
                TotalPrice = a.AppointmentsVaccines.FirstOrDefault()?.TotalPrice ?? 0,
                Notes = a.Notes
            }).ToList();

            return appointmentDTOs;
        }
        catch (Exception e)
        {
            _logger.Error($"Error fetching appointments for child ID {childId}: {e.Message}");
            throw;
        }
    }


    /// <summary>
    /// Lấy chi tiết cuộc hẹn gần nhất dựa trên ID của trẻ
    /// </summary>
    /// <param name="childId"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<AppointmentDTO> GetAppointmentDetailsByChildIdAsync(Guid childId)
    {
        try
        {
            _logger.Info($"Fetching appointment details for child ID: {childId}");

            // Kiểm tra nếu trẻ tồn tại
            var childExists = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (childExists == null)
            {
                _logger.Warn($"Child with ID {childId} not found.");
                throw new Exception("Child not found.");
            }

            // Lấy cuộc hẹn gần nhất của trẻ, Include AppointmentsVaccines và Vaccine
            var appointment = await _unitOfWork.AppointmentRepository.GetQueryable()
                .Where(a => a.ChildId == childId)
                .Include(a => a.AppointmentsVaccines) // Bao gồm danh sách vaccine
                .ThenInclude(av => av.Vaccine) // Nạp Vaccine đúng cách
                .FirstOrDefaultAsync(); // Lấy cuộc hẹn gần nhất

            if (appointment == null)
            {
                _logger.Warn($"No appointment found for child ID: {childId}");
                return null;
            }

            // Lấy thông tin vaccine từ AppointmentsVaccines
            var vaccine = appointment.AppointmentsVaccines.FirstOrDefault()?.Vaccine;

            _logger.Info($"Successfully retrieved appointment ID: {appointment.Id} for child ID: {childId}");

            // Chuyển đổi sang DTO
            var appointmentDto = new AppointmentDTO
            {
                AppointmentId = appointment.Id,
                ChildId = appointment.ChildId.Value,
                AppointmentDate = appointment.AppointmentDate.Value,
                Status = appointment.Status.ToString(),
                VaccineName = vaccine?.VaccineName ?? "Unknown", // ✅ Fix lỗi VaccineName
                DoseNumber = appointment.AppointmentsVaccines.FirstOrDefault()?.DoseNumber ?? 0,
                TotalPrice = appointment.AppointmentsVaccines.FirstOrDefault()?.TotalPrice ?? 0,
                Notes = appointment.Notes
            };

            return appointmentDto;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving appointment details for child ID {childId}: {ex.Message}");
            throw;
        }
    }
}