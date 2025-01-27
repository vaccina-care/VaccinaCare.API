using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.Application.Interface
{
    public interface IVaccineService
    {
        Task<Vaccine> CreateVaccine(VaccineDTO vaccineDTO);
        Task<Vaccine> DeleteVaccine(Guid id);
        Task<Vaccine> UpdateVaccine(Guid id, VaccineDTO vaccineDTO);
    }
}
