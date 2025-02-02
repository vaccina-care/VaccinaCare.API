using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IVaccineService
{
    Task<Vaccine> CreateVaccine(VaccineDTO vaccineDTO);
    Task<Vaccine> DeleteVaccine(Guid id);
    Task<VaccineDTO> UpdateVaccine(Guid id, VaccineDTO vaccineDTO);
    Task<PagedResult<VaccineDTO>> GetVaccines(
        string? search, string? type, string? sortBy, bool isDescending, int page, int pageSize);
}