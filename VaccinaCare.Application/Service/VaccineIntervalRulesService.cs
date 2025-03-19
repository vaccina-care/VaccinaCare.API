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
    ///     Kiểm tra xem vaccine này có thể tiêm chung với các vaccine khác hay không.
    ///     - Nếu vaccine có quy tắc "không thể tiêm chung" → trả về false.
    ///     - Nếu vaccine có yêu cầu khoảng cách tối thiểu giữa các lần tiêm,
    ///     kiểm tra lịch hẹn gần nhất của vaccine đã đặt trước đó.
    ///     - Nếu khoảng cách không đủ → trả về false.
    ///     - Nếu tất cả kiểm tra hợp lệ → trả về true.
    /// </summary>
    public async Task<bool> CheckVaccineCompatibility(Guid vaccineId, List<Guid> bookedVaccineIds,
        DateTime appointmentDate)
    {
        _logger.Info(
            $"[CheckVaccineCompatibility] Start checking for vaccine {vaccineId} with booked vaccines: {string.Join(", ", bookedVaccineIds)} on {appointmentDate}");

        foreach (var bookedVaccineId in bookedVaccineIds)
        {
            _logger.Info(
                $"Checking compatibility between VaccineId: {vaccineId} and BookedVaccineId: {bookedVaccineId}");

            // Lấy quy tắc tiêm giữa vaccine được chọn và các vaccine đã đặt lịch trước đó
            var rule = await _unitOfWork.VaccineIntervalRulesRepository
                .FirstOrDefaultAsync(r =>
                    (r.VaccineId == vaccineId && r.RelatedVaccineId == bookedVaccineId) ||
                    (r.VaccineId == bookedVaccineId && r.RelatedVaccineId == vaccineId));

            // Nếu có quy tắc xác định
            if (rule != null)
            {
                _logger.Info(
                    $"Found VaccineIntervalRule for {vaccineId} and {bookedVaccineId}: CanBeGivenTogether = {rule.CanBeGivenTogether}, MinIntervalDays = {rule.MinIntervalDays}");

                // Nếu hai loại vaccine không thể tiêm chung, trả về false
                if (!rule.CanBeGivenTogether)
                {
                    _logger.Info($"Vaccine {vaccineId} and {bookedVaccineId} cannot be given together.");
                    return false;
                }

                // Nếu có yêu cầu về khoảng cách tối thiểu giữa các mũi tiêm
                if (rule.MinIntervalDays > 0)
                {
                    // Kiểm tra lịch hẹn gần nhất của vaccine đã đặt trước đó
                    var lastAppointment = await _unitOfWork.AppointmentsVaccineRepository
                        .FirstOrDefaultAsync(a =>
                            a.VaccineId == bookedVaccineId &&
                            a.Appointment.AppointmentDate.HasValue &&
                            a.Appointment.AppointmentDate.Value.AddDays(rule.MinIntervalDays) > appointmentDate);

                    // Nếu có lịch hẹn vi phạm khoảng cách tối thiểu, từ chối lịch tiêm
                    if (lastAppointment != null)
                    {
                        _logger.Info(
                            $"Vaccine {vaccineId} must be scheduled at least {rule.MinIntervalDays} days after vaccine {bookedVaccineId}. Appointment denied.");
                        return false;
                    }
                }
            }
            else
            {
                _logger.Info(
                    $"No interval rule found between VaccineId: {vaccineId} and BookedVaccineId: {bookedVaccineId}. Assuming compatible.");
            }
        }

        _logger.Info($"[CheckVaccineCompatibility] Vaccine {vaccineId} is compatible with all booked vaccines.");
        return true;
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