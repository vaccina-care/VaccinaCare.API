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
    /// </summary>
    public async Task<(bool isCompatible, string message)> CheckVaccineCompatibility(Guid vaccineId,
        List<Guid> bookedVaccineIds,
        DateTime appointmentDate)
    {
        try
        {
            _logger.Info($"[CheckVaccineCompatibility] Start checking for vaccine {vaccineId}");

            // Get current vaccine name
            var currentVaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineId);
            if (currentVaccine == null)
            {
                return (false, $"Vaccine with id {vaccineId} not found");
            }

            foreach (var bookedVaccineId in bookedVaccineIds)
            {
                // Get booked vaccine name
                var bookedVaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(bookedVaccineId);
                if (bookedVaccine == null) continue;

                _logger.Info(
                    $"Checking compatibility between {currentVaccine.VaccineName} and {bookedVaccine.VaccineName}");

                var rule = await _unitOfWork.VaccineIntervalRulesRepository
                    .FirstOrDefaultAsync(r =>
                        (r.VaccineId == vaccineId && r.RelatedVaccineId == bookedVaccineId) ||
                        (r.VaccineId == bookedVaccineId && r.RelatedVaccineId == vaccineId));

                if (rule != null)
                {
                    if (!rule.CanBeGivenTogether)
                    {
                        string errorMessage =
                            $"Vaccine {currentVaccine.VaccineName} cannot be given together with {bookedVaccine.VaccineName}.";
                        _logger.Info(errorMessage);
                        return (false, errorMessage);
                    }

                    if (rule.MinIntervalDays > 0)
                    {
                        var lastAppointment = await _unitOfWork.AppointmentsVaccineRepository
                            .FirstOrDefaultAsync(a =>
                                a.VaccineId == bookedVaccineId &&
                                a.Appointment.AppointmentDate.HasValue &&
                                a.Appointment.AppointmentDate.Value.AddDays(rule.MinIntervalDays) > appointmentDate);

                        if (lastAppointment != null)
                        {
                            string errorMessage =
                                $"Vaccine {currentVaccine.VaccineName} must be scheduled at least {rule.MinIntervalDays} days after {bookedVaccine.VaccineName}. Appointment denied.";
                            _logger.Info(errorMessage);
                            return (false, errorMessage);
                        }
                    }
                }
            }

            return (true, "Vaccine is compatible with all booked vaccines.");
        }
        catch (Exception ex)
        {
            return (false, $"Error checking vaccine compatibility: {ex.Message}");
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