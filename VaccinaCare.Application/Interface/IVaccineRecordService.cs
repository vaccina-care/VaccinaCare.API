using VaccinaCare.Domain.DTOs.VaccineRecordDTOs;

namespace VaccinaCare.Application.Interface;

public interface IVaccineRecordService
{
    Task<int> GetRemainingDoses(Guid childId, Guid vaccineId);
    Task<VaccineRecordDto> AddVaccinationRecordAsync(AddVaccineRecordDto addVaccineRecordDto);
    Task<VaccineRecordDto> UpdateReactionDetails(UpdateVaccineRecorđto updateVaccineRecorDto);

    Task<VaccineRecordDto> GetRecordDetailsByIdAsync(Guid recordId);
    Task<List<VaccineRecordDto>> GetListRecordsByChildIdAsync(Guid parentId);
}