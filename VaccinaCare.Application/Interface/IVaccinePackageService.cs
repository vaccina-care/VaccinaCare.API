using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface;

public interface IVaccinePackageService
{
    Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO dto);
    Task<List<VaccinePackageDTO>> GetAllVaccinePackagesAsync();
    Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(Guid packageId);
    Task<bool> DeleteVaccinePackageByIdAsync(Guid packageId);
    Task<VaccinePackageDTO> UpdateVaccinePackageByIdAsync(Guid packageId, UpdateVaccinePackageDTO dto);
    Task<Pagination<VaccinePackageDTO>> GetVaccinePackagesPaging(PaginationParameter pagination);
}