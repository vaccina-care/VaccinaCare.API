using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;

namespace VaccinaCare.Application.Interface
{
    public interface IVaccinePackageService
    {
        Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO dto);
        Task<List<VaccinePackageDTO>> GetAllVaccinePackagesAsync();
        Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(Guid packageId);
    }
}
