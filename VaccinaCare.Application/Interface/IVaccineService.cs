using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineService
{
    Task<bool> IsVaccineInPackage(Guid childId, Guid vaccineId);

    Task<(bool isEligible, string message)> CanChildReceiveVaccine(Guid childId, Guid vaccineId);
    Task<int> GetNextDoseNumber(Guid childId, Guid vaccineId);

    Task<bool> CheckVaccineCompatibility(Guid vaccineId, List<Guid> bookedVaccineIds,
        DateTime appointmentDate);
    //CRUD
    Task<CreateVaccineDto> CreateVaccine(CreateVaccineDto vaccineDTO);
    Task<VaccineDTO> DeleteVaccine(Guid id);

    Task<VaccineDTO> UpdateVaccine(Guid id, VaccineDTO vaccineDTO);

    Task<PagedResult<VaccineDTO>> GetVaccines(string? search, string? type, string? sortBy, bool isDescending, int page,
        int pageSize);

    Task<VaccineDTO> GetVaccineById(Guid id);
}