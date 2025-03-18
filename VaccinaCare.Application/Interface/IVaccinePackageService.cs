using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccinePackageService
{
    Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO dto);

    Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(Guid packageId);

    Task<bool> DeleteVaccinePackageByIdAsync(Guid packageId);

    Task<VaccinePackageDTO> UpdateVaccinePackageByIdAsync(Guid packageId, UpdateVaccinePackageDTO dto);

    Task<PagedResult<VaccinePackageResultDTO>> GetAllVaccinesAndPackagesAsync(string? searchName,
        string? searchDescription, int pageNumber, int pageSize);

    Task<List<VaccinePackageDTO>> GetAllVaccinePackagesAsync();
    Task<VaccinePackage?> GetMostBookedPackageAsync();
}