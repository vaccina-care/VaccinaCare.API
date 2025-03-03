using System;
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

    public async Task<VaccineIntervalRulesDTO> CreateVaccineIntervalRuleAsync(Guid vaccineId, Guid? realatedVaccineId,
        int minIntervalDays, bool canBeGivenTogether)
    {
        _logerService.Info($"Creating Vaccine Interval Rules: ");

        if (vaccineId == Guid.Empty)
            throw new ArgumentException("VaccineId cannot be empty.");
        if (minIntervalDays <= 0)
            throw new ArgumentException("MinIntervalDays cannot be negative.");
        try
        {
            var rule = new VaccineIntervalRules
            {
                VaccineId = vaccineId,
                RelatedVaccineId = realatedVaccineId,
                MinIntervalDays = minIntervalDays,
                CanBeGivenTogether = canBeGivenTogether
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
}