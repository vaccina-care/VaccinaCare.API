using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.DTOs.VaccineRecordDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class AppointmentService : IAppointmentService
{
    private readonly IClaimsService _claimsService;
    private readonly IEmailService _emailService;
    private readonly ILoggerService _logger;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVaccineIntervalRulesService _vaccineIntervalRules;
    private readonly IVaccineRecordService _vaccineRecordService;
    private readonly IVaccineService _vaccineService;

    public AppointmentService(IUnitOfWork unitOfWork, ILoggerService loggerService,
        INotificationService notificationService, IVaccineService vaccineService, IEmailService emailService,
        IVaccineRecordService vaccineRecordService, IClaimsService claimsService,
        IVaccineIntervalRulesService vaccineIntervalRules)
    {
        _unitOfWork = unitOfWork;
        _logger = loggerService;
        _notificationService = notificationService;
        _vaccineService = vaccineService;
        _emailService = emailService;
        _vaccineRecordService = vaccineRecordService;
        _claimsService = claimsService;
        _vaccineIntervalRules = vaccineIntervalRules;
    }

    public async Task<List<AppointmentDTO>> UpdateAppointmentDate(Guid appointmentId, DateTime newDate)
    {
        try
        {
            var appointment = await _unitOfWork.AppointmentRepository
                .GetQueryable()
                .Include(a => a.AppointmentsVaccines)
                .ThenInclude(av => av.Vaccine)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                _logger.Error($"Appointment with Id={appointmentId} not found.");
                return null;
            }

            var oldDate = appointment.AppointmentDate;
            if (!oldDate.HasValue)
            {
                _logger.Error($"Appointment with Id={appointmentId} không có ngày hẹn hợp lệ.");
                return null;
            }

            // Kiểm tra VaccineId
            var vaccineId = appointment.AppointmentsVaccines.FirstOrDefault()?.VaccineId ?? Guid.Empty;
            if (vaccineId == Guid.Empty)
            {
                _logger.Error($"Cannot determine VaccineId for Appointment {appointmentId}.");
                return null;
            }

            // Không cho phép dời lịch vào quá khứ
            if (newDate < DateTime.UtcNow.Date)
            {
                _logger.Error($"Cannot reschedule to a past date: {newDate}");
                return null;
            }

            // Tính khoảng cách chênh lệch
            var dateDiff = newDate - oldDate.Value;

            // Cập nhật ngày cho appointment hiện tại
            appointment.AppointmentDate = newDate;
            await _unitOfWork.AppointmentRepository.Update(appointment);

            // Dời tất cả appointment sau đó với cùng khoảng cách chênh lệch
            var subsequentAppointments = await _unitOfWork.AppointmentRepository
                .GetQueryable()
                .Where(a => a.ChildId == appointment.ChildId
                            && a.Id != appointment.Id
                            && a.AppointmentDate.HasValue
                            && a.AppointmentDate.Value > oldDate.Value)
                .Include(a => a.AppointmentsVaccines)
                .ThenInclude(av => av.Vaccine)
                .ToListAsync();

            foreach (var subAppt in subsequentAppointments)
                subAppt.AppointmentDate = subAppt.AppointmentDate!.Value.Add(dateDiff);

            await _unitOfWork.AppointmentRepository.UpdateRange(subsequentAppointments);
            await _unitOfWork.SaveChangesAsync();

            // Chuyển tất cả appointment thành List<AppointmentDTO> để trả về
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

            if (appointment == null) throw new Exception("Appointment not found.");

            // Kiểm tra trạng thái hiện tại của appointment
            if (appointment.Status != AppointmentStatus.Confirmed)
                throw new Exception("Appointment can only be updated if it is in Confirmed status.");

            // Xử lý cập nhật trạng thái
            switch (newStatus)
            {
                case AppointmentStatus.Completed:
                    appointment.Status = AppointmentStatus.Completed;

                    // Thêm record tiêm chủng vào hồ sơ của trẻ
                    foreach (var appointmentVaccine in appointment.AppointmentsVaccines)
                    {
                        var addVaccineRecordDto = new AddVaccineRecordDto
                        {
                            ChildId = appointment.ChildId,
                            VaccineId = appointmentVaccine.VaccineId ?? throw new Exception("VaccineId is missing"),
                            VaccinationDate = appointment.AppointmentDate ?? DateTime.UtcNow,
                            DoseNumber = appointmentVaccine.DoseNumber ?? 1,
                            ReactionDetails = null
                        };

                        await _vaccineRecordService.AddVaccinationRecordAsync(addVaccineRecordDto);
                    }

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
            var bookedVaccineIds = await _unitOfWork.AppointmentsVaccineRepository
                .GetAllAsync(av =>
                    av.Appointment.ChildId == request.ChildId && av.Appointment.Status != AppointmentStatus.Cancelled)
                .ContinueWith(task => task.Result.Select(av => av.VaccineId.Value).Distinct().ToList());

            _logger.Info($"Child {request.ChildId} has booked vaccines: {string.Join(", ", bookedVaccineIds)}");

            // Kiểm tra tính tương thích của vaccine với danh sách vaccine đã đặt lịch
            var (isCompatible, compatibilityMessage) = await _vaccineIntervalRules.CheckVaccineCompatibility(
                request.VaccineId, bookedVaccineIds, request.StartDate);

            if (!isCompatible)
            {
                _logger.Warn(compatibilityMessage);
                throw new ArgumentException(compatibilityMessage);
            }

            _logger.Info(
                $"[CheckVaccineCompatibility] Vaccine {request.VaccineId} is compatible with all booked vaccines.");

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
                    await _emailService.SendSingleAppointmentConfirmationAsync(emailRequest, appointment,
                        request.VaccineId);
            }

            //PHASE 11: GỬI NOTI CHO USER
            if (appointments.Any())
                await _notificationService.PushNotificationAppointmentSuccess(parentId, appointments.First().Id);

            // PHASE 12: CHUYỂN ĐỔI DỮ LIỆU SANG DTO VÀ TRẢ VỀ KẾT QUẢ
            var appointmentDTOs = new List<AppointmentDTO>();

            foreach (var a in appointments)
            {
                // Lấy thông tin của child
                var child = await _unitOfWork.ChildRepository.GetByIdAsync(a.ChildId);

                appointmentDTOs.Add(new AppointmentDTO
                {
                    AppointmentId = a.Id,
                    ChildId = a.ChildId,
                    ChildName = child?.FullName ?? "Unknown",
                    UserId = parentId,
                    UserName = user?.FullName ?? "Unknown",
                    AppointmentDate = a.AppointmentDate.Value,
                    Status = a.Status.ToString(),
                    VaccineName = a.AppointmentsVaccines.First().Vaccine?.VaccineName ?? "Unknown",
                    DoseNumber = a.AppointmentsVaccines.First().DoseNumber ?? 0,
                    TotalPrice = a.AppointmentsVaccines.First().TotalPrice ?? 0,
                    Notes = a.Notes
                });
            }

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

                //if (recentAppointments.Any())
                //    throw new ArgumentException(
                //        $"Trẻ đã có lịch hẹn tiêm {vaccine.VaccineName} gần đây. Vui lòng chọn ngày khác.");

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

            //Noti
            foreach (var appointment in appointments)
                await _notificationService.PushNotificationAppointmentSuccess(parentId, appointment.Id);

            // Lấy thông tin của child trước vòng lặp
            var child = await _unitOfWork.ChildRepository.GetByIdAsync(request.ChildId);

            var appointmentDTOs = appointments.Select(a => new AppointmentDTO
            {
                AppointmentId = a.Id,
                ChildId = a.ChildId,
                ChildName = child?.FullName ?? "Unknown",
                UserId = parentId,
                UserName = user?.FullName ?? "Unknown",
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

            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);

            var appointmentDTOs = appointments.Select(a => new AppointmentDTO
            {
                AppointmentId = a.Id,
                ChildId = a.ChildId,
                ChildName = child?.FullName ?? "Unknown",
                UserId = a.ParentId,
                AppointmentDate = a.AppointmentDate.Value,
                Status = a.Status.ToString(),
                VaccineName = a.AppointmentsVaccines.FirstOrDefault()?.Vaccine?.VaccineName ?? "Unknown",
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

    public async Task<Pagination<AppointmentDTO>> GetAllAppointments(
        PaginationParameter pagination,
        string? searchTerm = null,
        AppointmentStatus? status = null)
    {
        try
        {
            var query = _unitOfWork.AppointmentRepository.GetQueryable()
                .Include(a => a.AppointmentsVaccines)
                .ThenInclude(av => av.Vaccine)
                .Include(a => a.Child)
                .AsQueryable();

            // Filter by status if provided
            if (status.HasValue) query = query.Where(a => a.Status == status.Value);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(a =>
                    a.AppointmentsVaccines.Any(av => av.Vaccine.VaccineName.Contains(searchTerm)) ||
                    a.AppointmentDate.ToString().Contains(searchTerm) ||
                    a.Child.FullName.Contains(searchTerm));

            var totalCount = await query.CountAsync();
            var appointments = await query
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var appointmentDTOs = new List<AppointmentDTO>();

            foreach (var app in appointments)
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(app.ParentId);

                appointmentDTOs.Add(new AppointmentDTO
                {
                    AppointmentId = app.Id,
                    UserId = app.ParentId,
                    UserName = user?.FullName ?? "Unknown",
                    ChildId = app.ChildId,
                    ChildName = app.Child?.FullName ?? "Unknown", // Use included Child directly
                    AppointmentDate = app.AppointmentDate ?? DateTime.MinValue,
                    Status = app.Status.ToString(),
                    VaccineName = app.AppointmentsVaccines?.FirstOrDefault()?.Vaccine?.VaccineName ?? string.Empty,
                    DoseNumber = app.AppointmentsVaccines?.FirstOrDefault()?.DoseNumber ?? 0,
                    TotalPrice = app.AppointmentsVaccines?.Sum(av => av.TotalPrice) ?? 0,
                    Notes = app.Notes
                });
            }

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