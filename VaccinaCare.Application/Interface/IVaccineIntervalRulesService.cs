using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

namespace VaccinaCare.Application.Interface
{
    public interface IVaccineIntervalRulesService
    {
        Task<VaccineIntervalRulesDTO> CreateVaccineIntervalRuleAsync(Guid vaccineId, Guid? realatedVaccineId, int minIntervalDays, bool canBeGivenTogether);
    }
}
