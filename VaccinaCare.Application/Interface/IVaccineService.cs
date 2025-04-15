using Microsoft.AspNetCore.Http;
using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineService
{
    Task<(bool isEligible, string message)> CanChildReceiveVaccine(Guid childId, Guid vaccineId);

    //CRUD
    Task<VaccineDto> CreateVaccine(CreateVaccineDto createVaccineDto, IFormFile vaccinePictureFile);

    Task<VaccineDto> DeleteVaccine(Guid id);

    Task<VaccineDto> UpdateVaccine(Guid id, UpdateVaccineDto updateDto, IFormFile? vaccinePictureFile);

    Task<PagedResult<VaccineDto>> GetVaccines(string? search, string? type, string? sortBy, bool isDescending, int page,
        int pageSize);

    Task<VaccineDto> GetVaccineById(Guid id);
    Task<int> GetVaccineAvailable();
    Task<List<VaccineBookingDto>> GetTop5MostBookedVaccinesAsync();
}