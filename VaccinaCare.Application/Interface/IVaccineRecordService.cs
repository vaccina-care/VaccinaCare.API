using VaccinaCare.Domain.DTOs.VaccineDTOs.VaccineRecord;

namespace VaccinaCare.Application.Interface;

public interface IVaccineRecordService
{
    Task<VaccineRecordDto> AddVaccinationRecordAsync(AddVaccineRecordDto addVaccineRecordDto);
}