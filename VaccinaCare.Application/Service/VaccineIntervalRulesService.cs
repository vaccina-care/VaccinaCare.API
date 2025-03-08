using Microsoft.VisualBasic;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineIntervalRulesService : IVaccineIntervalRulesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logerService;
    private readonly IClaimsService _claimsService;

    public VaccineIntervalRulesService(IUnitOfWork unitOfWork, ILoggerService logerService,
        IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _logerService = logerService;
        _claimsService = claimsService;
    }

    public async Task<VaccineIntervalRulesDTO> CreateVaccineIntervalRuleAsync(
        VaccineIntervalRulesDTO vaccineIntervalRulesDTO)
    {
        _logerService.Info($"Creating Vaccine Interval Rules: ");

        if (vaccineIntervalRulesDTO.VaccineId == Guid.Empty)
            throw new ArgumentException("VaccineId cannot be empty.");
        if (vaccineIntervalRulesDTO.MinIntervalDays < 0)
            throw new ArgumentException("MinIntervalDays cannot be negative.");
        try
        {
            if (vaccineIntervalRulesDTO.CanBeGivenTogether)
            {
                vaccineIntervalRulesDTO.MinIntervalDays = 0;
            }

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
            _logerService.Error($"Error in CreateVaccineIntervalRuleAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteVaccineIntervalRuleAsync(Guid id)
    {
        _logerService.Info($"Attempting to delete Vaccine Interval Rule wiht ID: {id}");
        try
        {
            var vaccineIntervalRule = await _unitOfWork.VaccineIntervalRulesRepository.GetByIdAsync(id);

            if (vaccineIntervalRule == null)
            {
                _logerService.Warn($"Vaccine Interlval Rule with ID {id} not found.");
                return false;
            }

            await _unitOfWork.VaccineIntervalRulesRepository.SoftRemove(vaccineIntervalRule);
            await _unitOfWork.SaveChangesAsync();

            _logerService.Info($"Successfully deleted Vaccine Interval Rule with ID: {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logerService.Error($"Error deleting Vaccine Interval Rule with ID {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<List<GetVaccineInternalRulesDto>> GetAllVaccineIntervalRulesAsync()
    {
        try
        {
            _logerService.Info($"Fetching all Vaccine Interval Rules....");

            var vaccineIntervalRules = await _unitOfWork.VaccineIntervalRulesRepository.GetAllAsync();

            var result = vaccineIntervalRules.Select(v => new GetVaccineInternalRulesDto
            {
                Id = v.Id,
                VaccineId = v.VaccineId,
                RelatedVaccineId = v.RelatedVaccineId,
                CanBeGivenTogether = v.CanBeGivenTogether,
                MinIntervalDays = v.MinIntervalDays
            }).ToList();

            _logerService.Info($"Fetched {result.Count} Vaccine Interval Rules successfully.");
            return result;
        }
        catch (Exception ex)
        {
            _logerService.Error($"Error fetching Vaccine Interval Rules: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccineIntervalRulesDTO> UpdateVaccineIntervalRuleAsync(Guid id,
        VaccineIntervalRulesDTO updateDto)
    {
        try
        {
            _logerService.Info($"Updating Vaccine Interval Rule with ID: {id}");

            if (updateDto == null)
            {
                _logerService.Warn("Update data is null.");
                throw new ArgumentNullException(nameof(updateDto), "Update data can be null.");
            }

            var vaccineIntervalRule = await _unitOfWork.VaccineIntervalRulesRepository.GetByIdAsync(id);
            if (vaccineIntervalRule == null)
            {
                _logerService.Warn($"Vaccine Interval Rule with ID {id} not found.");
                return null;
            }

            if (updateDto.VaccineId == Guid.Empty || updateDto.RelatedVaccineId == Guid.Empty)
            {
                _logerService.Warn("VaccineId or RelatedVaccineId is empty.");
                throw new ArgumentException("VaccineId and RelatedVaccineId cannot be empty.");
            }

            if (updateDto.VaccineId == updateDto.RelatedVaccineId)
            {
                _logerService.Warn("VaccineId and RelatedVaccineId cannot be the same.");
                throw new ArgumentException("A vaccine cannot have an interval rule wiht itseft.");
            }

            if (updateDto.MinIntervalDays < 0)
            {
                _logerService.Info("MinIntervalDay cannot be neagative.");
                throw new ArgumentException("MinIntervalDay must be a non-negative.");
            }

            if (updateDto.CanBeGivenTogether && updateDto.MinIntervalDays > 0)
            {
                _logerService.Warn("If vaccines can be give together, MinIntervalDays should be 0.");
                throw new ArgumentException("If CanBeGivenTogether is true, MinIntervalDays must be 0.");
            }

            vaccineIntervalRule.VaccineId = updateDto.VaccineId;
            vaccineIntervalRule.RelatedVaccineId = updateDto.RelatedVaccineId;
            vaccineIntervalRule.MinIntervalDays = updateDto.MinIntervalDays;
            vaccineIntervalRule.CanBeGivenTogether = updateDto.CanBeGivenTogether;

            await _unitOfWork.VaccineIntervalRulesRepository.Update(vaccineIntervalRule);
            await _unitOfWork.SaveChangesAsync();

            _logerService.Info($"Update vaccine interval rule with id {id} successfully.");

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
            _logerService.Error($"Error updating vaccine interval rule: {ex.Message}");
            throw;
        }
    }
}