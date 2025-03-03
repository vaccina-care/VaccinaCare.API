using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineRecordService : IVaccineRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _logger;

    public VaccineRecordService(IUnitOfWork unitOfWork, IClaimsService claimsService, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _logger = logger;
    }

    public async Task AddVaccinationRecordAsync(Guid childId, Guid vaccineId, DateTime vaccinationDate, int doseNumber)
    {
        try
        {
            var vaccinationRecord = new VaccinationRecord
            {
                ChildId = childId,
                VaccineId = vaccineId,
                VaccinationDate = vaccinationDate,
                DoseNumber = doseNumber
            };

            await _unitOfWork.VaccinationRecordRepository.AddAsync(vaccinationRecord);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info(
                $"[Success] Vaccination record added for ChildId: {childId}, VaccineId: {vaccineId}, Dose: {doseNumber}");
        }
        catch (Exception ex)
        {
            _logger.Info(
                $"[Error] Failed to add vaccination record for ChildId: {childId}, VaccineId: {vaccineId}, Dose: {doseNumber}. Error: {ex.Message}");
            throw new Exception("Lỗi khi thêm hồ sơ tiêm chủng. Vui lòng thử lại sau.");
        }
    }
}