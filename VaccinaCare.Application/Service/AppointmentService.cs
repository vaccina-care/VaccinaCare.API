using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
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

    public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService,
        IVaccineService vaccineService, IVaccineRecordService vaccineRecordService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _claimsService = claimsService;
        _vaccineService = vaccineService;
        _vaccineRecordService = vaccineRecordService;
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

    /// <summary>
    /// Tạo danh sách nhiều Appointments dựa trên số liều, số vaccine và khoảng cách giữa các mũi.
    /// VD: 1 Vaccine có 3 mũi, thì phải tạo 3 appointment ứng với 3 mũi.  
    /// </summary>
    public async Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentDto request,
        Guid parentId)
    {
        var appointments = new List<Appointment>();

        if (await _vaccineService.IsVaccineInPackage(request.VaccineIds))
            throw new Exception("Tất cả vaccine này thuộc gói đã đăng ký. Vui lòng đặt theo gói để có giá ưu đãi hơn.");

        foreach (var vaccineId in request.VaccineIds)
        {
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineId);
            if (vaccine == null)
                throw new Exception($"Vaccine với ID {vaccineId} không tồn tại.");

            var (isEligible, message) = await _vaccineService.CanChildReceiveVaccine(request.ChildId, vaccineId);
            if (!isEligible)
                throw new Exception($"Trẻ không đủ điều kiện tiêm vaccine {vaccine.VaccineName}: {message}");

            var nextDose = await _vaccineService.GetNextDoseNumber(request.ChildId, vaccineId);
            if (nextDose > vaccine.RequiredDoses)
                throw new Exception($"Trẻ đã tiêm đủ số mũi của vaccine {vaccine.VaccineName}.");

            var appointmentDate = request.StartDate;

            for (var dose = nextDose; dose <= vaccine.RequiredDoses; dose++)
            {
                var bookedVaccineIds = appointments
                    .SelectMany(a => a.AppointmentsVaccines.Select(av => av.VaccineId)).ToList();
                if (!await _vaccineService.CheckVaccineCompatibility(vaccineId, bookedVaccineIds, appointmentDate))
                    throw new Exception(
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
                            VaccineId = vaccineId,
                            DoseNumber = dose,
                            TotalPrice = vaccine.Price
                        }
                    }
                };

                appointments.Add(appointment);
                appointmentDate = appointmentDate.AddDays(vaccine.DoseIntervalDays);

                // **Thêm dữ liệu vào bảng VaccinationRecord**
                await _vaccineRecordService.AddVaccinationRecordAsync(request.ChildId, vaccineId, appointmentDate,
                    dose);
            }
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