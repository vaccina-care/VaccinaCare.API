using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.DTOs.VaccineDTOs.VaccineRecord;
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

    

    public async Task<VaccineRecordDto> AddVaccinationRecordAsync(AddVaccineRecordDto addVaccineRecordDto)
    {
        try
        {
            // Validation business rules
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(addVaccineRecordDto.VaccineId);
            if (vaccine == null)
                throw new Exception("Vaccine not found.");

            var child = await _unitOfWork.ChildRepository.GetByIdAsync(addVaccineRecordDto.ChildId);
            if (child == null)
                throw new Exception("Child not found.");

            if (addVaccineRecordDto.DoseNumber > vaccine.RequiredDoses)
                throw new Exception("Dose number exceeds the required doses for this vaccine.");

            var vaccinationRecord = new VaccinationRecord
            {
                ChildId = addVaccineRecordDto.ChildId,
                VaccineId = addVaccineRecordDto.VaccineId,
                VaccinationDate = addVaccineRecordDto.VaccinationDate,
                DoseNumber = addVaccineRecordDto.DoseNumber,
                ReactionDetails = addVaccineRecordDto.ReactionDetails
            };

            // Add to the database
            await _unitOfWork.VaccinationRecordRepository.AddAsync(vaccinationRecord);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"[Success] Vaccination record added for ChildId: {addVaccineRecordDto.ChildId}, VaccineId: {addVaccineRecordDto.VaccineId}, Dose: {addVaccineRecordDto.DoseNumber}");

            // Prepare DTO to return
            var vaccineRecordDto = new VaccineRecordDto
            {
                ChildId = vaccinationRecord.ChildId,
                VaccineId = vaccinationRecord.VaccineId,
                VaccinationDate = vaccinationRecord.VaccinationDate,
                ReactionDetails = vaccinationRecord.ReactionDetails,
                DoseNumber = vaccinationRecord.DoseNumber,
                VaccineName = vaccine.VaccineName,
                ChildFullName = child.FullName
            };

            return vaccineRecordDto;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while adding vaccination record: {ex.Message}");
            throw new Exception("An error occurred while adding the vaccination record. Please try again later.");
        }
    }
}