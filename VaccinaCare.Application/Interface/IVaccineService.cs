using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineService
{
    Task<CreateVaccineDTO> CreateVaccine(CreateVaccineDTO vaccineDTO);
    Task<VaccineDTO> DeleteVaccine(Guid id);
    Task<VaccineDTO> UpdateVaccine(Guid id, VaccineDTO vaccineDTO);
    Task<PagedResult<VaccineDTO>> GetVaccines(
        string? search, string? type, string? sortBy, bool isDescending, int page, int pageSize);
    Task<VaccineDTO> GetVaccineById(Guid id);

    Task<bool> CanBeAdministeredTogether(Guid vaccine1Id, Guid vaccine2Id);
    Task<int> GetMinIntervalDays(Guid vaccine1Id, Guid vaccine2Id);
    Task<decimal> GetVaccinePrice(Guid vaccineId);
}