using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineIntervalRulesService
{
    Task<VaccineIntervalRulesDTO> CreateVaccineIntervalRuleAsync(VaccineIntervalRulesDTO vaccineIntervalRulesDTO);
    Task<List<GetVaccineInternalRulesDto>> GetAllVaccineIntervalRulesAsync();
    Task<bool> DeleteVaccineIntervalRuleAsync(Guid id);
    Task<VaccineIntervalRulesDTO> UpdateVaccineIntervalRuleAsync(Guid id, VaccineIntervalRulesDTO updateDto);
    Task<bool> CheckVaccineCompatibility(Guid vaccineId, List<Guid> bookedVaccineIds, DateTime appointmentDate);
}