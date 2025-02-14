using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IVaccineService
{
    Task<VaccineDTO> CreateVaccine(VaccineDTO vaccineDTO);
    Task<VaccineDTO> DeleteVaccine(Guid id);
    Task<VaccineDTO> UpdateVaccine(Guid id, VaccineDTO vaccineDTO);
    Task<PagedResult<VaccineDTO>> GetVaccines(
        string? search, string? type, string? sortBy, bool isDescending, int page, int pageSize);
    Task<VaccineDTO> GetVaccineById(Guid id);
}