namespace VaccinaCare.Application.Interface;

public interface IVaccineRecordService
{
    Task AddVaccinationRecordAsync(Guid childId, Guid vaccineId, DateTime vaccinationDate, int doseNumber);
}