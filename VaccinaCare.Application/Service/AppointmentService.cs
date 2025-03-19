using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class AppointmentService : IAppointmentService
{
    private readonly IEmailService _emailService;
    private readonly ILoggerService _logger;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVaccineRecordService _vaccineRecordService;
    private readonly IVaccineService _vaccineService;
    private readonly IClaimsService _claimsService;

    public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService,
        INotificationService notificationService, IVaccineService vaccineService, IEmailService emailService,
        IVaccineRecordService vaccineRecordService, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _notificationService = notificationService;
        _vaccineService = vaccineService;
        _emailService = emailService;
        _vaccineRecordService = vaccineRecordService;
        _claimsService = claimsService;
    }

    public async Task<AppointmentDTO> UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus,
        string? cancellationReason = null)
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
                throw new Exception("Appointment not found.");
            }

            // Kiểm tra trạng thái hiện tại của appointment
            if (appointment.Status != AppointmentStatus.Confirmed)
            {
                throw new Exception("Appointment can only be updated if it is in Confirmed status.");
            }

            // Cập nhật trạng thái bằng switch-case
            switch (newStatus)
            {
                case AppointmentStatus.Completed:
                    appointment.Status = AppointmentStatus.Completed;
                    break;

                case AppointmentStatus.Cancelled:
                    appointment.Status = AppointmentStatus.Cancelled;
                    appointment.CancellationReason = cancellationReason;
                    break;

                default:
                    throw new Exception("Invalid status update. You can only update to Completed or Cancelled.");
            }

            // Lưu thay đổi vào database
            await _unitOfWork.AppointmentRepository.Update(appointment);
            await _unitOfWork.SaveChangesAsync();

            // Trả về dữ liệu cập nhật
            return new AppointmentDTO
            {
                AppointmentId = appointment.Id,
                ChildId = appointment.ChildId,
                UserId = appointment.ParentId,
                AppointmentDate = appointment.AppointmentDate ?? DateTime.MinValue,
                Status = appointment.Status.ToString(),
                VaccineName = appointment.AppointmentsVaccines.FirstOrDefault()?.Vaccine?.VaccineName ?? "Unknown",
                DoseNumber = appointment.AppointmentsVaccines.FirstOrDefault()?.DoseNumber ?? 0,
                TotalPrice = appointment.AppointmentsVaccines.FirstOrDefault()?.TotalPrice ?? 0,
                Notes = appointment.Notes ?? string.Empty
            };
        }
        catch (Exception e)
        {
            _logger.Error($"Error updating appointment status: {e.Message}");
            throw;
        }
    }


    public async Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(
        CreateAppointmentSingleVaccineDto request,
        Guid parentId)
    {
        try
        {
            // PHASE 1: BẮT ĐẦU QUÁ TRÌNH TẠO LỊCH TIÊM
            _logger.Info(
                $"[Start] Generating appointments for vaccine {request.VaccineId} for child {request.ChildId}");

            // PHASE 2: LẤY THÔNG TIN VACCINE
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(request.VaccineId);
            if (vaccine == null)
            {
                _logger.Error($"Vaccine với ID {request.VaccineId} không tồn tại.");
                throw new ArgumentException($"Vaccine với ID {request.VaccineId} không tồn tại.");
            }

            _logger.Info($"Vaccine {vaccine.VaccineName} requires {vaccine.RequiredDoses} doses.");

            // PHASE 3: KIỂM TRA SỐ LIỀU CÒN LẠI CỦA TRẺ
            var remainingDoses = await _vaccineRecordService.GetRemainingDoses(request.ChildId, request.VaccineId);
            _logger.Info($"Child {request.ChildId} has {remainingDoses} doses remaining.");

            // PHASE 4: NẾU ĐÃ TIÊM ĐỦ, KHÔNG TẠO LỊCH
            if (remainingDoses <= 0)
            {
                _logger.Info(
                    $"Child {request.ChildId} đã hoàn thành tất cả các mũi tiêm cho vaccine {vaccine.VaccineName}.");
                return new List<AppointmentDTO>();
            }

            // PHASE 5: KIỂM TRA TÍNH ĐỦ ĐIỀU KIỆN TIÊM VACCINE
            var (isEligible, eligibilityMessage) =
                await _vaccineService.CanChildReceiveVaccine(request.ChildId, request.VaccineId);
            if (!isEligible)
            {
                _logger.Warn(eligibilityMessage);
                return new List<AppointmentDTO>();
            }

            // PHASE 6: KIỂM TRA TÍNH TƯƠNG THÍCH CỦA VACCINE
            var bookedVaccineIds =
                new List<Guid>(); // List of vaccines already booked by the child (this might need to be fetched from the database)
            var isCompatible =
                await _vaccineService.CheckVaccineCompatibility(request.VaccineId, bookedVaccineIds, request.StartDate);
            if (!isCompatible)
            {
                _logger.Warn($"Vaccine {request.VaccineId} is not compatible with one or more booked vaccines.");
                return new List<AppointmentDTO>();
            }

            // PHASE 7: XÁC ĐỊNH SỐ MŨI ĐÃ TIÊM & TÍNH TOÁN SỐ LỊCH CẦN TẠO
            var hasPreviousRecords = remainingDoses != vaccine.RequiredDoses;
            var completedDoses = hasPreviousRecords ? vaccine.RequiredDoses - remainingDoses : 0;

            _logger.Info(
                $"Total required doses: {vaccine.RequiredDoses}, Completed: {completedDoses}, Creating {remainingDoses} appointments.");

            // PHASE 8: TẠO DANH SÁCH CÁC LỊCH HẸN CÒN LẠI
            var appointments = new List<Appointment>();
            var appointmentDate = request.StartDate;

            for (var dose = completedDoses + 1; dose <= vaccine.RequiredDoses; dose++)
            {
                _logger.Info($"Creating appointment for dose {dose} on {appointmentDate}");

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

            _logger.Info($"Total {appointments.Count} appointments created.");

            // PHASE 9: LƯU CÁC LỊCH HẸN VÀO DATABASE
            await _unitOfWork.AppointmentRepository.AddRangeAsync(appointments);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info("Appointments saved to the database.");

            // PHASE 10: GỬI EMAIL CHO USER
            var user = await _unitOfWork.UserRepository.GetByIdAsync(parentId);
            if (user != null)
            {
                var emailRequest = new EmailRequestDTO
                {
                    UserEmail = user.Email,
                    UserName = user.FullName
                };

                // Send email for each appointment
                foreach (var appointment in appointments)
                {
                    await _emailService.SendSingleAppointmentConfirmationAsync(emailRequest, appointment,
                        request.VaccineId);
                }
            }

            // PHASE 11: CHUYỂN ĐỔI DỮ LIỆU SANG DTO VÀ TRẢ VỀ KẾT QUẢ
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

    public async Task<Pagination<AppointmentDTO>> GetAllAppointments(PaginationParameter pagination,
        string? searchTerm = null)
    {
        try
        {
            var userId = _claimsService.GetCurrentUserId;
            // Build the query to include vaccine details
            var query = _unitOfWork.AppointmentRepository.GetQueryable()
                .Include(a => a.AppointmentsVaccines) // Include vaccines for each appointment
                .ThenInclude(av => av.Vaccine)
                .AsQueryable();

            // Apply search filter if a search term is provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a =>
                    a.AppointmentsVaccines.Any(av => av.Vaccine.VaccineName.Contains(searchTerm)) ||
                    a.AppointmentDate.ToString().Contains(searchTerm));
            }

            // Apply pagination
            var totalCount = await query.CountAsync();
            var appointments = await query
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            // Map Appointment to AppointmentDTO
            var appointmentDTOs = appointments.Select(app => new AppointmentDTO
            {
                AppointmentId = app.Id,
                UserId = app.ParentId,
                ChildId = app.ChildId,
                AppointmentDate = app.AppointmentDate ?? DateTime.MinValue,
                Status = app.Status.ToString(),
                VaccineName = app.AppointmentsVaccines?.FirstOrDefault()?.Vaccine?.VaccineName ?? string.Empty,
                DoseNumber = app.AppointmentsVaccines?.FirstOrDefault()?.DoseNumber ?? 0,
                TotalPrice = app.AppointmentsVaccines?.Sum(av => av.TotalPrice) ?? 0,
                Notes = app.Notes
            }).ToList();

            // Return paginated result
            return new Pagination<AppointmentDTO>(appointmentDTOs, totalCount, pagination.PageIndex,
                pagination.PageSize);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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