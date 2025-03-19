﻿using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineRecordDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineRecordService : IVaccineRecordService
{
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public VaccineRecordService(IUnitOfWork unitOfWork, IClaimsService claimsService, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _logger = logger;
    }

    public async Task<int> GetRemainingDoses(Guid childId, Guid vaccineId)
    {
        try
        {
            _logger.Info($"Fetching remaining doses for Child {childId} and Vaccine {vaccineId}");

            // 1️⃣ Lấy thông tin vaccine
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineId);
            if (vaccine == null)
            {
                _logger.Error($"Vaccine với ID {vaccineId} không tồn tại.");
                throw new Exception("Vaccine không tồn tại.");
            }

            _logger.Info($"Vaccine {vaccine.VaccineName} requires {vaccine.RequiredDoses} doses.");

            // 2️⃣ Chỉ lấy số liều đã tiêm của đúng VaccineId này
            var existingRecords = await _unitOfWork.VaccinationRecordRepository
                .GetAllAsync(vr => vr.ChildId == childId && vr.VaccineId == vaccineId); // ✅ Đảm bảo đúng dữ liệu

            var dosesTaken = existingRecords.Count;
            _logger.Info($"Child {childId} đã tiêm {dosesTaken} liều của vaccine {vaccine.VaccineName}.");

            // 3️⃣ Tính số liều còn lại
            var remainingDoses = vaccine.RequiredDoses - dosesTaken;

            if (remainingDoses <= 0)
            {
                _logger.Info($"Child {childId} đã hoàn thành tất cả các mũi tiêm cho vaccine {vaccine.VaccineName}.");
                return 0;
            }

            _logger.Info($"Child {childId} cần {remainingDoses} liều nữa của vaccine {vaccine.VaccineName}.");

            return remainingDoses;
        }
        catch (Exception ex)
        {
            _logger.Error($"Lỗi khi lấy số liều còn lại: {ex.Message}");
            throw;
        }
    }


    public async Task<VaccineRecordDto> GetRecordDetailsByIdAsync(Guid recordId)
    {
        try
        {
            // Fetch the vaccination record by recordId
            var vaccinationRecord = await _unitOfWork.VaccinationRecordRepository
                .FirstOrDefaultAsync(vr => vr.Id == recordId);

            // If record not found, throw an exception
            if (vaccinationRecord == null)
                throw new Exception("Vaccination record not found for the specified recordId.");

            // Map the VaccinationRecord entity to VaccineRecordDto
            var vaccineRecordDto = new VaccineRecordDto
            {
                Id = vaccinationRecord.Id,
                ChildId = vaccinationRecord.ChildId,
                VaccineId = vaccinationRecord.VaccineId,
                VaccinationDate = vaccinationRecord.VaccinationDate.Value, // Assuming VaccinationDate is not null
                ReactionDetails = vaccinationRecord.ReactionDetails,
                DoseNumber = vaccinationRecord.DoseNumber
            };

            return vaccineRecordDto;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching vaccination record for RecordId: {recordId}. Exception: {ex.Message}");
            throw new Exception("Error while fetching vaccination record. Please try again later.");
        }
    }

    public async Task<List<VaccineRecordDto>> GetListRecordsByChildIdAsync(Guid childId)
    {
        try
        {
            // Fetch the list of vaccination records for the given ChildId
            var vaccinationRecords = await _unitOfWork.VaccinationRecordRepository
                .GetAllAsync(vr => vr.ChildId == childId);

            // If no records found, return an empty list
            if (vaccinationRecords == null || !vaccinationRecords.Any()) return new List<VaccineRecordDto>();

            // Map the VaccinationRecord entities to VaccineRecordDto list
            var vaccineRecordDtos = vaccinationRecords.Select(vr => new VaccineRecordDto
            {
                Id = vr.Id,
                ChildId = vr.ChildId,
                VaccineId = vr.VaccineId,
                VaccinationDate = vr.VaccinationDate.Value, // Assuming VaccinationDate is not null
                ReactionDetails = vr.ReactionDetails,
                DoseNumber = vr.DoseNumber
            }).ToList();

            return vaccineRecordDtos;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching vaccination records for ChildId: {childId}. Exception: {ex.Message}");
            throw new Exception("Error while fetching vaccination records. Please try again later.");
        }
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

            _logger.Info(
                $"[Success] Vaccination record added for ChildId: {addVaccineRecordDto.ChildId}, VaccineId: {addVaccineRecordDto.VaccineId}, Dose: {addVaccineRecordDto.DoseNumber}");

            // Prepare DTO to return
            var vaccineRecordDto = new VaccineRecordDto
            {
                Id = vaccinationRecord.Id,
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