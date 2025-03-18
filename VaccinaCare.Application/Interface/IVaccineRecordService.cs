using VaccinaCare.Domain.DTOs.VaccineRecordDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineRecordService
{
    Task<VaccineRecordDto> AddVaccinationRecordAsync(AddVaccineRecordDto addVaccineRecordDto);

    Task<VaccineRecordDto> GetRecordDetailsByIdAsync(Guid recordId);

    Task<List<VaccineRecordDto>> GetListRecordsByChildIdAsync(Guid parentId);
}