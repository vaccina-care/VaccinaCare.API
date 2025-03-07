using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineIntervalRulesService
{
    Task<VaccineIntervalRulesDTO> CreateVaccineIntervalRuleAsync(VaccineIntervalRulesDTO vaccineIntervalRulesDTO);
    Task<List<VaccineIntervalRulesDTO>> GetAllVaccineIntervalRulesAsync();
    Task<bool> DeleteVaccineIntervalRuleAsync(Guid id);
    Task<VaccineIntervalRulesDTO> UpdateVaccineIntervalRuleAsync(Guid id, VaccineIntervalRulesDTO updateDto);

}