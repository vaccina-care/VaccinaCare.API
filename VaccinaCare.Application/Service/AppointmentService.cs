using LinqKit;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
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
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;


    public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService,
        INotificationService notificationService, IVaccineService vaccineService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _notificationService = notificationService;
        _vaccineService = vaccineService;
        _emailService = emailService;
    }

    // public async Task<List<AppointmentDTO>> GenerateAppointmentsForConsultant(Guid parentId, DateTime startDate)
    // {
    //     try
    //     {
    //     }
    //     catch (Exception e)
    //     {
    //         throw;
    //     }
    // }


    public async Task<List<AppointmentDTO>> UpdateAppointmentDate(Guid appointmentId, DateTime newDate)
    {
        try
        {
            var appointment = await _unitOfWork.AppointmentRepository
                .GetQueryable()
                .Include(a => a.AppointmentsVaccines)
                .ThenInclude(av => av.Vaccine) // Include thông tin vaccine
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                var errorMsg = $"Appointment with Id={appointmentId} not found.";
                _logger.Error(errorMsg);
                return null; // Không tìm thấy appointment
            }

            var oldDate = appointment.AppointmentDate;
            if (!oldDate.HasValue)
            {
                var errorMsg = $"Appointment with Id={appointmentId} không có ngày hẹn hợp lệ.";
                _logger.Error(errorMsg);
                return null;
            }

            // 1️⃣ Kiểm tra appointment trước đó đã được Confirmed chưa
            var previousAppointment = await _unitOfWork.AppointmentRepository
                .GetQueryable()
                .Where(a => a.ChildId == appointment.ChildId
                            && a.Id != appointment.Id
                            && a.AppointmentDate.HasValue
                            && a.AppointmentDate.Value < oldDate.Value)
                .OrderByDescending(a => a.AppointmentDate)
                .FirstOrDefaultAsync();

            if (previousAppointment != null && previousAppointment.Status != AppointmentStatus.Confirmed)
            {
                var errorMsg =
                    $"Cannot reschedule Appointment {appointmentId} because previous Appointment {previousAppointment.Id} is not Confirmed.";
                _logger.Error(errorMsg);
                return null;
            }

            // 2️⃣ Kiểm tra VaccineId
            var vaccineId = appointment.AppointmentsVaccines.FirstOrDefault()?.VaccineId ?? Guid.Empty;
            if (vaccineId == Guid.Empty)
            {
                var errorMsg = $"Cannot determine VaccineId for Appointment {appointmentId}.";
                _logger.Error(errorMsg);
                return null;
            }

            // 3️⃣ Không cho phép dời lịch vào quá khứ
            if (newDate < DateTime.UtcNow.Date)
            {
                var errorMsg = $"Cannot reschedule to a past date: {newDate}";
                _logger.Error(errorMsg);
                return null;
            }

            // 4️⃣ Tính khoảng cách chênh lệch
            var dateDiff = newDate - oldDate.Value;

            // 5️⃣ Cập nhật ngày cho appointment hiện tại
            appointment.AppointmentDate = newDate;
            await _unitOfWork.AppointmentRepository.Update(appointment);

            // 6️⃣ Dời tất cả appointment sau đó với cùng khoảng cách chênh lệch
            var subsequentAppointments = await _unitOfWork.AppointmentRepository
                .GetQueryable()
                .Where(a => a.ChildId == appointment.ChildId
                            && a.Id != appointment.Id
                            && a.AppointmentDate.HasValue
                            && a.AppointmentDate.Value > oldDate.Value)
                .Include(a => a.AppointmentsVaccines)
                .ThenInclude(av => av.Vaccine) // Include vaccine data
                .ToListAsync();

            foreach (var subAppt in subsequentAppointments)
                subAppt.AppointmentDate = subAppt.AppointmentDate!.Value.Add(dateDiff);

            await _unitOfWork.AppointmentRepository.UpdateRange(subsequentAppointments);
            await _unitOfWork.SaveChangesAsync();

            // 7️⃣ Chuyển tất cả appointment thành `List<AppointmentDTO>` để trả về
            var updatedAppointments = new List<AppointmentDTO>();
            var allAppointments = new List<Appointment> { appointment }.Concat(subsequentAppointments);

            foreach (var appt in allAppointments)
            {
                var vaccineInfo = appt.AppointmentsVaccines.FirstOrDefault();
                updatedAppointments.Add(new AppointmentDTO
                {
                    AppointmentId = appt.Id,
                    ChildId = appt.ChildId,
                    AppointmentDate = appt.AppointmentDate ?? DateTime.MinValue,
                    Status = appt.Status.ToString(),
                    VaccineName = vaccineInfo?.Vaccine?.VaccineName ?? "Unknown",
                    DoseNumber = vaccineInfo?.DoseNumber ?? 0,
                    TotalPrice = vaccineInfo?.TotalPrice ?? 0,
                    Notes = appt.Notes ?? ""
                });
            }

            // 8️⃣ Logging
            _logger.Info($"Appointment {appointmentId} has been rescheduled from {oldDate} to {newDate}. " +
                         $"Also updated {subsequentAppointments.Count} subsequent appointment(s).");

            return updatedAppointments;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error when updating appointment date: {ex.Message}");
            return null;
        }
    }

    public async Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(
        CreateAppointmentSingleVaccineDto request,
        Guid parentId)
    {
        try
        {
            var appointments = new List<Appointment>();

            // Lấy vaccine
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(request.VaccineId);
            if (vaccine == null) throw new ArgumentException($"Vaccine với ID {request.VaccineId} không tồn tại.");

            _logger.Info($"Found vaccine: {vaccine.VaccineName}, Required Doses: {vaccine.RequiredDoses}");

            // Kiểm tra đủ điều kiện tiêm chưa
            var (isEligible, message) =
                await _vaccineService.CanChildReceiveVaccine(request.ChildId, request.VaccineId);
            if (!isEligible)
            {
                _logger.Error($"Trẻ không đủ điều kiện tiêm vaccine {vaccine.VaccineName}: {message}");
                throw new ArgumentException($"Trẻ không đủ điều kiện tiêm vaccine {vaccine.VaccineName}: {message}");
            }

            // Lấy số mũi tiêm tiếp theo
            var nextDose = await _vaccineService.GetNextDoseNumber(request.ChildId, request.VaccineId);
            if (nextDose > vaccine.RequiredDoses)
            {
                _logger.Error($"Trẻ đã tiêm đủ số mũi của vaccine {vaccine.VaccineName}.");
                throw new ArgumentException($"Trẻ đã tiêm đủ số mũi của vaccine {vaccine.VaccineName}.");
            }

            _logger.Info($"Next dose for child {request.ChildId}: {nextDose}");

            var now = DateTime.UtcNow;
            var blockIntervalDays = 3;

            // Kiểm tra các lịch hẹn gần đây cho vaccine này
            var recentAppointmentsSameVaccine = await _unitOfWork.AppointmentRepository.GetQueryable()
                .Include(a => a.AppointmentsVaccines)
                .Where(a => !a.IsDeleted
                            && a.ChildId == request.ChildId
                            && a.AppointmentsVaccines.Any(av => av.VaccineId == request.VaccineId)
                            && a.AppointmentDate >= now.AddDays(-blockIntervalDays)
                            && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            if (recentAppointmentsSameVaccine.Any())
            {
                _logger.Warn($"Trẻ đã có lịch hẹn tiêm {vaccine.VaccineName} gần đây.");
                throw new ArgumentException(
                    $"Trẻ đã có lịch hẹn tiêm {vaccine.VaccineName} gần đây. Vui lòng chọn ngày khác hoặc chờ đủ khoảng cách.");
            }

            // Lấy danh sách các mũi tiêm đã tiêm (dựa trên VaccinationRecord)
            var vaccinationRecords = await _unitOfWork.VaccinationRecordRepository
                .GetAllAsync(vr => vr.ChildId == request.ChildId && vr.VaccineId == request.VaccineId);

            // Tạo danh sách các mũi tiêm đã tiêm
            var completedDoses = vaccinationRecords
                .Where(vr => vr.DoseNumber <= vaccine.RequiredDoses)
                .Select(vr => vr.DoseNumber)
                .ToList();

            // Tính số mũi tiêm còn lại
            var remainingDoses = vaccine.RequiredDoses - completedDoses.Count;

            if (remainingDoses <= 0)
            {
                _logger.Info($"Trẻ đã tiêm đầy đủ {vaccine.RequiredDoses} mũi vaccine {vaccine.VaccineName}.");
                throw new ArgumentException(
                    $"Trẻ đã tiêm đủ {vaccine.RequiredDoses} mũi của vaccine {vaccine.VaccineName}.");
            }

            var appointmentDate = request.StartDate;

            // Kiểm tra sự tương thích giữa vaccine mới và vaccine đã đặt trước
            var bookedVaccineIds = await _unitOfWork.AppointmentsVaccineRepository
                .GetQueryable()
                .Where(av => !av.IsDeleted
                             && av.Appointment.ChildId == request.ChildId
                             && av.Appointment.Status != AppointmentStatus.Cancelled)
                .Select(av => av.VaccineId.Value)
                .Distinct()
                .ToListAsync();

            if (!await _vaccineService.CheckVaccineCompatibility(request.VaccineId, bookedVaccineIds, appointmentDate))
            {
                _logger.Error($"Vaccine {vaccine.VaccineName} không thể tiêm cùng các loại vaccine đã đặt trước.");
                throw new ArgumentException(
                    $"Vaccine {vaccine.VaccineName} không thể tiêm cùng các loại vaccine đã đặt trước.");
            }

            // Tạo các appointments cho vaccine này
            for (var dose = nextDose; dose < nextDose + remainingDoses; dose++)
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
                _logger.Info(
                    $"Created appointment for dose {dose} of vaccine {vaccine.VaccineName} on {appointment.AppointmentDate.Value:yyyy-MM-dd}");
            }

            _logger.Info($"Total appointments created: {appointments.Count}");

            await _unitOfWork.AppointmentRepository.AddRangeAsync(appointments);
            await _unitOfWork.SaveChangesAsync();

            // Gửi thông tin email xác nhận
            var user = await _unitOfWork.UserRepository.GetByIdAsync(parentId);
            if (user != null)
            {
                var emailRequest = new EmailRequestDTO
                {
                    UserEmail = user.Email,
                    UserName = user.FullName
                };

                foreach (var appointment in appointments)
                {
                    _logger.Info($"Sending email confirmation for appointment ID {appointment.Id} to {user.Email}");
                    await _emailService.SendSingleAppointmentConfirmationAsync(emailRequest, appointment);
                }
            }

            // Gửi thông báo push
            foreach (var appointment in appointments)
            {
                _logger.Info($"Sending push notification for appointment ID {appointment.Id}.");
                await _notificationService.PushNotificationAppointmentSuccess(parentId, appointment.Id);
            }

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

            return appointmentDTOs;
        }
        catch (ArgumentException ex)
        {
            _logger.Error($"Argument error: {ex.Message}");
            throw new ArgumentException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error: {ex.Message}");
            throw new Exception("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
        }
    }


    public async Task<List<AppointmentDTO>> GenerateAppointmentsForPackageVaccine(
        CreateAppointmentPackageVaccineDto request, Guid parentId)
    {
        try
        {
            var appointments = new List<Appointment>();

            // Lấy gói vaccine
            var vaccinePackage = await _unitOfWork.VaccinePackageRepository
                .GetQueryable()
                .Include(vp => vp.VaccinePackageDetails)
                .ThenInclude(vpd => vpd.Service)
                .FirstOrDefaultAsync(vp => vp.Id == request.PackageId);

            if (vaccinePackage == null)
                throw new ArgumentException($"Gói vaccine với ID {request.PackageId} không tồn tại.");

            // Lấy danh sách vaccine trong gói
            var packageDetails = vaccinePackage.VaccinePackageDetails
                .Where(vpd => vpd.Service != null)
                .Select(vpd => vpd.Service!)
                .Distinct()
                .ToList();

            if (!packageDetails.Any())
                throw new ArgumentException($"Gói vaccine {vaccinePackage.PackageName} không chứa vaccine nào.");

            var appointmentDate = request.StartDate;
            var blockIntervalDays = 3;

            foreach (var vaccine in packageDetails)
            {
                var (isEligible, message) = await _vaccineService.CanChildReceiveVaccine(request.ChildId, vaccine.Id);
                if (!isEligible)
                    throw new ArgumentException(
                        $"Trẻ không đủ điều kiện tiêm vaccine {vaccine.VaccineName}: {message}");

                var recentAppointments = await _unitOfWork.AppointmentRepository.GetQueryable()
                    .Include(a => a.AppointmentsVaccines)
                    .Where(a => !a.IsDeleted
                                && a.ChildId == request.ChildId
                                && a.AppointmentsVaccines.Any(av => av.VaccineId == vaccine.Id)
                                && a.AppointmentDate >= appointmentDate.AddDays(-blockIntervalDays)
                                && a.Status != AppointmentStatus.Cancelled)
                    .ToListAsync();

                if (recentAppointments.Any())
                    throw new ArgumentException(
                        $"Trẻ đã có lịch hẹn tiêm {vaccine.VaccineName} gần đây. Vui lòng chọn ngày khác.");

                for (var doseNumber = 1; doseNumber <= vaccine.RequiredDoses; doseNumber++)
                {
                    var appointment = new Appointment
                    {
                        ParentId = parentId,
                        ChildId = request.ChildId,
                        AppointmentDate = appointmentDate,
                        Status = AppointmentStatus.Pending,
                        VaccineType = VaccineType.Package,
                        Notes = $"Mũi {doseNumber}/{vaccine.RequiredDoses} - {vaccine.VaccineName}",
                        AppointmentsVaccines = new List<AppointmentsVaccine>
                        {
                            new()
                            {
                                VaccineId = vaccine.Id,
                                DoseNumber = doseNumber,
                                TotalPrice = vaccine.Price
                            }
                        }
                    };

                    appointments.Add(appointment);
                    appointmentDate = appointmentDate.AddDays(vaccine.DoseIntervalDays);
                }
            }

            await _unitOfWork.AppointmentRepository.AddRangeAsync(appointments);
            await _unitOfWork.SaveChangesAsync();

            // Gửi email xác nhận
            var user = await _unitOfWork.UserRepository.GetByIdAsync(parentId);
            if (user != null)
            {
                var emailRequest = new EmailRequestDTO
                {
                    UserEmail = user.Email,
                    UserName = user.FullName
                };

                await _emailService.SendPackageAppointmentConfirmationAsync(emailRequest, appointments,
                    request.PackageId);
            }


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

            return appointmentDTOs;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(ex.Message);
        }
        catch (Exception ex)
        {
            throw new Exception("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
        }
    }


    public async Task<List<AppointmentDTO>> GetListlAppointmentsByChildIdAsync(Guid childId)
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

            var appointments = await _unitOfWork.AppointmentRepository.GetQueryable()
                .Where(a => a.ChildId == childId)
                .Include(a => a.AppointmentsVaccines) // Bao gồm danh sách vaccine
                .ThenInclude(av => av.Vaccine)
                .ToListAsync();

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

    public async Task<AppointmentDTO> GetAppointmentDetailsByIdAsync(Guid appointmentId)
    {
        try
        {
            // Lấy cuộc hẹn dựa trên appointmentId, bao gồm thông tin AppointmentsVaccines và Vaccine
            var appointment = await _unitOfWork.AppointmentRepository.GetQueryable()
                .Where(a => a.Id == appointmentId)
                .Include(a => a.AppointmentsVaccines) // Bao gồm danh sách vaccine
                .ThenInclude(av => av.Vaccine) // Nạp thông tin Vaccine
                .FirstOrDefaultAsync(); // Lấy cuộc hẹn

            if (appointment == null)
            {
                _logger.Warn($"No appointment found for appointment ID: {appointmentId}");
                return null;
            }

            // Lấy thông tin vaccine từ AppointmentsVaccines (chỉ lấy vaccine đầu tiên nếu có)
            var vaccine = appointment.AppointmentsVaccines.FirstOrDefault()?.Vaccine;

            _logger.Info($"Successfully retrieved appointment ID: {appointment.Id}.");

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
            _logger.Error($"Error retrieving appointment details for appointment ID {appointmentId}: {ex.Message}");
            throw;
        }
    }
}