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

    public async Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentDto request,
        Guid parentId)
    {
        try
        {
            _logger.Info($"[START] Đang xử lý đặt lịch cho ChildID: {request.ChildId}, VaccineID: {request.VaccineId}");

            var appointments = new List<Appointment>();

            // Lấy vaccine
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(request.VaccineId);
            if (vaccine == null)
            {
                _logger.Error($"[ERROR] Vaccine với ID {request.VaccineId} không tồn tại.");
                throw new ArgumentException($"Vaccine với ID {request.VaccineId} không tồn tại.");
            }

            // Kiểm tra đủ điều kiện tiêm chưa
            var (isEligible, message) =
                await _vaccineService.CanChildReceiveVaccine(request.ChildId, request.VaccineId);
            if (!isEligible)
            {
                _logger.Warn($"[WARN] Trẻ không đủ điều kiện tiêm {vaccine.VaccineName}: {message}");
                throw new ArgumentException($"Trẻ không đủ điều kiện tiêm vaccine {vaccine.VaccineName}: {message}");
            }

            // Lấy số mũi tiêm tiếp theo
            var nextDose = await _vaccineService.GetNextDoseNumber(request.ChildId, request.VaccineId);
            _logger.Info($"[DOSE] Next Dose for {vaccine.VaccineName}: {nextDose}");

            if (nextDose > vaccine.RequiredDoses)
            {
                _logger.Warn($"[WARN] Trẻ đã tiêm đủ số mũi của vaccine {vaccine.VaccineName}.");
                throw new ArgumentException($"Trẻ đã tiêm đủ số mũi của vaccine {vaccine.VaccineName}.");
            }

            // -------------------------------------------------------
            // **Chỉ chặn nếu ‘cùng vaccine’ đã được đặt quá gần nhau** 
            // -------------------------------------------------------
            // Ví dụ: Lấy danh sách các lịch hẹn cũ (chưa tiêm hoặc sắp tiêm) mà VaccineId == request.VaccineId
            // và có thể thêm điều kiện DateTime, Status,... tuỳ nhu cầu

            var now = DateTime.UtcNow;
            // tuỳ theo bạn muốn cấm spam trong khoảng bao nhiêu ngày, ví dụ 3 ngày
            var blockIntervalDays = 3;

            var recentAppointmentsSameVaccine = await _unitOfWork.AppointmentRepository.GetQueryable()
                .Include(a => a.AppointmentsVaccines)
                .Where(a => !a.IsDeleted
                            && a.ChildId == request.ChildId
                            // Vaccine trùng với loại vaccine đang muốn đặt
                            && a.AppointmentsVaccines.Any(av => av.VaccineId == request.VaccineId)
                            // Có thể thêm điều kiện khoảng cách cho “spam” 
                            && a.AppointmentDate >= now.AddDays(-blockIntervalDays)
                            // Tránh tính những lịch hẹn đã huỷ, hoặc Completed... 
                            && a.Status != AppointmentStatus.Cancelled
                )
                .ToListAsync();

            if (recentAppointmentsSameVaccine.Any())
            {
                // Chỉ chặn nếu chính vaccine này đang bị “spam” 
                throw new ArgumentException(
                    $"Trẻ đã có lịch hẹn tiêm {vaccine.VaccineName} gần đây. Vui lòng chọn ngày khác hoặc chờ đủ khoảng cách.");
            }

            // -------------------------------------------------------
            // **Kiểm tra xung đột với các vaccine khác** 
            // -------------------------------------------------------
            // bookedVaccineIds ở đâu đó bạn đã lấy ra (các vaccine đã đặt trước đó)
            // Hàm này dùng để check conflict loại khác
            var appointmentDate = request.StartDate;
            _logger.Info($"[INFO] Appointment Date Selected: {appointmentDate}");


            // Lấy danh sách vaccine đã được đặt (đang pending/chưa bị hủy) cho Child này
            var bookedVaccineIds = await _unitOfWork.AppointmentsVaccineRepository
                .GetQueryable()
                .Where(av => !av.IsDeleted
                             // Join sang bảng Appointment để lọc theo ChildId
                             && av.Appointment.ChildId == request.ChildId
                             // Tuỳ ý chỉ lấy các Appointment có trạng thái không phải Cancelled
                             // (hoặc có thể bỏ điều kiện này, tuỳ logic bạn muốn)
                             && av.Appointment.Status != AppointmentStatus.Cancelled
                )
                // Lấy VaccineId
                .Select(av => av.VaccineId.Value)
                // Bỏ trùng lặp
                .Distinct()
                .ToListAsync();

            // Kiểm tra nếu vaccine này xung đột với các vaccine khác 
            // (nếu list bookedVaccineIds đang có vaccine Xung Đột)
            if (!await _vaccineService.CheckVaccineCompatibility(request.VaccineId, bookedVaccineIds, appointmentDate))
            {
                _logger.Warn($"[CONFLICT] Vaccine {vaccine.VaccineName} có xung đột với vaccine trước đó.");
                throw new ArgumentException(
                    $"Vaccine {vaccine.VaccineName} không thể tiêm cùng các loại vaccine đã đặt trước.");
            }

            _logger.Success($"[SUCCESS] Vaccine {vaccine.VaccineName} không có xung đột! Tiếp tục đặt lịch.");

            for (var dose = nextDose; dose <= vaccine.RequiredDoses; dose++)
            {
                var appointment = new Appointment
                {
                    ParentId = parentId,
                    ChildId = request.ChildId,
                    AppointmentDate = appointmentDate,
                    Status = AppointmentStatus.Pending,
                    VaccineType = VaccineType.SingleDose,
                    Notes = $"Mũi {dose}/{vaccine.RequiredDoses} - {vaccine.VaccineName}",
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
                ChildId = a.ChildId,
                AppointmentDate = a.AppointmentDate.Value,
                Status = a.Status.ToString(),
                VaccineName = a.AppointmentsVaccines.First().Vaccine?.VaccineName ?? "Unknown",
                DoseNumber = a.AppointmentsVaccines.First().DoseNumber ?? 0,
                TotalPrice = a.AppointmentsVaccines.First().TotalPrice ?? 0,
                Notes = a.Notes
            }).ToList();

            _logger.Success(
                $"[COMPLETE] Đặt lịch thành công cho {vaccine.VaccineName} - {appointmentDTOs.Count} lịch.");
            return appointmentDTOs;
        }
        catch (ArgumentException ex)
        {
            _logger.Warn($"[EXCEPTION] {ex.Message}");
            throw new ArgumentException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"[ERROR] Unexpected error in GenerateAppointments: {ex.Message}");
            throw new Exception("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
        }
    }


    public async Task<List<AppointmentDTO>> GetListlAppointmentsByChildIdAsync(Guid childId)
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
                ChildId = a.ChildId,
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
                ChildId = appointment.ChildId,
                AppointmentDate = appointment.AppointmentDate.Value,
                Status = appointment.Status.ToString(),
                VaccineName = vaccine?.VaccineName ?? "Unknown",
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