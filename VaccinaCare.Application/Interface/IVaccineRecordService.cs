using VaccinaCare.Domain.DTOs.VaccineDTOs.VaccineRecord;

namespace VaccinaCare.Application.Interface;

public interface IVaccineRecordService
{
    Task<VaccineRecordDto> AddVaccinationRecordAsync(AddVaccineRecordDto addVaccineRecordDto);
    Task<VaccineRecordDto> GetVaccinationRecordByRecordIdAsync(Guid recordId);
    Task<List<VaccineRecordDto>> GetListVaccinationRecordByChildIdAsync(Guid parentId);
}