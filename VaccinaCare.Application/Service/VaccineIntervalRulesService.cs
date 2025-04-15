using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineIntervalRulesService : IVaccineIntervalRulesService
{
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public VaccineIntervalRulesService(IUnitOfWork unitOfWork, ILoggerService logger,
        IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimsService = claimsService;
    }

    /// <summary>
    ///     Check if this vaccine can be administered together with other vaccines.
    ///     - If the vaccine has a rule "cannot be given together" → return false.
    ///     - If the vaccine has a minimum interval requirement between injections,
    ///     check the most recent appointment of the previously booked vaccine.
    ///     - If the interval is insufficient → return false.
    ///     - If all checks are valid → return true.
    /// </summary>
    public async Task<bool> CheckVaccineCompatibility(Guid vaccineId, List<Guid> bookedVaccineIds,
        DateTime appointmentDate)
    {
        try
        {
            _logger.Info(
                $"[CheckVaccineCompatibility] Start checking for vaccine {vaccineId} with booked vaccines: {string.Join(", ", bookedVaccineIds)} on {appointmentDate}");

            if (vaccineId == Guid.Empty)
            {
                throw new ArgumentException("Invalid Vaccine ID");
            }

            if (bookedVaccineIds == null || !bookedVaccineIds.Any())
            {
                return true; // No previously booked vaccines, so no conflicts
            }

            // Lấy thông tin vaccine được chọn để đưa vào thông báo lỗi
            var selectedVaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineId);
            if (selectedVaccine == null)
            {
                throw new ArgumentException($"Vaccine information not found for ID: {vaccineId}");
            }

            foreach (var bookedVaccineId in bookedVaccineIds)
            {
                try
                {
                    // Lấy thông tin vaccine đã đặt lịch để đưa vào thông báo lỗi
                    var bookedVaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(bookedVaccineId);
                    if (bookedVaccine == null)
                    {
                        throw new ArgumentException($"Booked vaccine information not found for ID: {bookedVaccineId}");
                    }

                    // Lấy quy tắc tiêm giữa vaccine được chọn và các vaccine đã đặt lịch trước đó
                    var rule = await _unitOfWork.VaccineIntervalRulesRepository
                        .FirstOrDefaultAsync(r =>
                            (r.VaccineId == vaccineId && r.RelatedVaccineId == bookedVaccineId) ||
                            (r.VaccineId == bookedVaccineId && r.RelatedVaccineId == vaccineId));

                    // Nếu có quy tắc xác định
                    if (rule != null)
                    {
                        // Nếu hai loại vaccine không thể tiêm chung, trả về false
                        if (!rule.CanBeGivenTogether)
                        {
                            throw new InvalidOperationException(
                                $"Vaccine {selectedVaccine.VaccineName} cannot be administered together with vaccine {bookedVaccine.VaccineName}");
                        }

                        // Nếu có yêu cầu về khoảng cách tối thiểu giữa các mũi tiêm
                        if (rule.MinIntervalDays > 0)
                        {
                            // Kiểm tra lịch hẹn gần nhất của vaccine đã đặt trước đó
                            var lastAppointment = await _unitOfWork.AppointmentsVaccineRepository
                                .FirstOrDefaultAsync(a =>
                                    a.VaccineId == bookedVaccineId &&
                                    a.Appointment.AppointmentDate.HasValue &&
                                    a.Appointment.AppointmentDate.Value.AddDays(rule.MinIntervalDays) >
                                    appointmentDate);

                            // Nếu có lịch hẹn vi phạm khoảng cách tối thiểu, từ chối lịch tiêm
                            if (lastAppointment != null)
                            {
                                var earliestDate =
                                    lastAppointment.Appointment.AppointmentDate.Value.AddDays(rule.MinIntervalDays);
                                throw new InvalidOperationException(
                                    $"Vaccine {selectedVaccine.VaccineName} must be administered at least {rule.MinIntervalDays} days after vaccine {bookedVaccine.VaccineName}. " +
                                    $"The earliest possible date is: {earliestDate:MM/dd/yyyy}");
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger.Error($"Error checking compatibility with vaccine {bookedVaccineId}: {ex.Message}");
                    throw new InvalidOperationException(
                        $"Unable to check compatibility with booked vaccine: {ex.Message}");
                }
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            // Chuyển tiếp exception message chi tiết về vaccine xung đột
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Undefined error when checking vaccine compatibility: {ex.Message}");
            throw new InvalidOperationException($"Error checking vaccine compatibility: {ex.Message}");
        }
    }

    public async Task<VaccineIntervalRulesDTO> CreateVaccineIntervalRuleAsync(
        VaccineIntervalRulesDTO vaccineIntervalRulesDTO)
    {
        _logger.Info("Creating Vaccine Interval Rules: ");

        if (vaccineIntervalRulesDTO.VaccineId == Guid.Empty)
            throw new ArgumentException("VaccineId cannot be empty.");
        if (vaccineIntervalRulesDTO.MinIntervalDays < 0)
            throw new ArgumentException("MinIntervalDays cannot be negative.");
        try
        {
            if (vaccineIntervalRulesDTO.CanBeGivenTogether) vaccineIntervalRulesDTO.MinIntervalDays = 0;

            var rule = new VaccineIntervalRules
            {
                VaccineId = vaccineIntervalRulesDTO.VaccineId,
                RelatedVaccineId = vaccineIntervalRulesDTO.RelatedVaccineId,
                MinIntervalDays = vaccineIntervalRulesDTO.MinIntervalDays,
                CanBeGivenTogether = vaccineIntervalRulesDTO.CanBeGivenTogether
            };

            await _unitOfWork.VaccineIntervalRulesRepository.AddAsync(rule);
            await _unitOfWork.SaveChangesAsync();

            return new VaccineIntervalRulesDTO
            {
                VaccineId = rule.VaccineId,
                RelatedVaccineId = rule.RelatedVaccineId,
                MinIntervalDays = rule.MinIntervalDays,
                CanBeGivenTogether = rule.CanBeGivenTogether
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in CreateVaccineIntervalRuleAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<GetVaccineInternalRulesDto>> GetAllVaccineIntervalRulesAsync()
    {
        try
        {
            _logger.Info("Fetching all Vaccine Interval Rules....");

            var vaccineIntervalRules = await _unitOfWork.VaccineIntervalRulesRepository.GetAllAsync();

            var result = vaccineIntervalRules.Select(v => new GetVaccineInternalRulesDto
            {
                VaccineIntervalRUID = v.Id,
                VaccineId = v.VaccineId,
                RelatedVaccineId = v.RelatedVaccineId,
                CanBeGivenTogether = v.CanBeGivenTogether,
                MinIntervalDays = v.MinIntervalDays
            }).ToList();

            _logger.Info($"Fetched {result.Count} Vaccine Interval Rules successfully.");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching Vaccine Interval Rules: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccineIntervalRulesDTO> UpdateVaccineIntervalRuleAsync(Guid id,
        VaccineIntervalRulesDTO updateDto)
    {
        try
        {
            _logger.Info($"Updating Vaccine Interval Rule with ID: {id}");

            if (updateDto == null)
            {
                _logger.Warn("Update data is null.");
                throw new ArgumentNullException(nameof(updateDto), "Update data can be null.");
            }

            var vaccineIntervalRule = await _unitOfWork.VaccineIntervalRulesRepository.GetByIdAsync(id);
            if (vaccineIntervalRule == null)
            {
                _logger.Warn($"Vaccine Interval Rule with ID {id} not found.");
                return null;
            }

            if (updateDto.VaccineId == Guid.Empty || updateDto.RelatedVaccineId == Guid.Empty)
            {
                _logger.Warn("VaccineId or RelatedVaccineId is empty.");
                throw new ArgumentException("VaccineId and RelatedVaccineId cannot be empty.");
            }

            if (updateDto.VaccineId == updateDto.RelatedVaccineId)
            {
                _logger.Warn("VaccineId and RelatedVaccineId cannot be the same.");
                throw new ArgumentException("A vaccine cannot have an interval rule wiht itseft.");
            }

            if (updateDto.MinIntervalDays < 0)
            {
                _logger.Info("MinIntervalDay cannot be neagative.");
                throw new ArgumentException("MinIntervalDay must be a non-negative.");
            }

            if (updateDto.CanBeGivenTogether && updateDto.MinIntervalDays > 0)
            {
                _logger.Warn("If vaccines can be give together, MinIntervalDays should be 0.");
                throw new ArgumentException("If CanBeGivenTogether is true, MinIntervalDays must be 0.");
            }

            vaccineIntervalRule.VaccineId = updateDto.VaccineId;
            vaccineIntervalRule.RelatedVaccineId = updateDto.RelatedVaccineId;
            vaccineIntervalRule.MinIntervalDays = updateDto.MinIntervalDays;
            vaccineIntervalRule.CanBeGivenTogether = updateDto.CanBeGivenTogether;

            await _unitOfWork.VaccineIntervalRulesRepository.Update(vaccineIntervalRule);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Update vaccine interval rule with id {id} successfully.");

            return new VaccineIntervalRulesDTO
            {
                VaccineId = vaccineIntervalRule.VaccineId,
                RelatedVaccineId = vaccineIntervalRule.RelatedVaccineId,
                MinIntervalDays = vaccineIntervalRule.MinIntervalDays,
                CanBeGivenTogether = vaccineIntervalRule.CanBeGivenTogether
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating vaccine interval rule: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteVaccineIntervalRuleAsync(Guid id)
    {
        _logger.Info($"Attempting to delete Vaccine Interval Rule wiht ID: {id}");
        try
        {
            var vaccineIntervalRule = await _unitOfWork.VaccineIntervalRulesRepository.GetByIdAsync(id);

            if (vaccineIntervalRule == null)
            {
                _logger.Warn($"Vaccine Interlval Rule with ID {id} not found.");
                return false;
            }

            await _unitOfWork.VaccineIntervalRulesRepository.SoftRemove(vaccineIntervalRule);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Successfully deleted Vaccine Interval Rule with ID: {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting Vaccine Interval Rule with ID {id}: {ex.Message}");
            return false;
        }
    }
}