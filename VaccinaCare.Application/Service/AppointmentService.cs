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
    /// Tạo danh sách nhiều Appointments dựa trên số liều, số vaccine và khoảng cách giữa các mũi.
    /// VD: 1 Vaccine có 3 mũi, thì phải tạo 3 APpointment ứng với 3 mũi.  
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
}