using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class ChildService : IChildService
{
    private readonly ILoggerService _loggerService;
    private readonly IClaimsService _claimsService;
    private readonly IUnitOfWork _unitOfWork;

    public ChildService(ILoggerService loggerService, IUnitOfWork unitOfWork, IClaimsService claimsService)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
    }

    public async Task<ChildDto> CreateChildAsync(CreateChildDto childDto)
    {
        try
        {
            // Get ParentId from ClaimsService
            Guid parentId = _claimsService.GetCurrentUserId;

            _loggerService.Info($"Starting child profile creation for parent {parentId}");

            // Check if the parent exists
            var parent = await _unitOfWork.UserRepository.GetByIdAsync(parentId);
            if (parent == null)
            {
                _loggerService.Warn($"Parent {parentId} does not exist.");
                throw new KeyNotFoundException("Parent not found.");
            }

            // Create a new Child entity from DTO
            var child = new Child
            {
                Id = Guid.NewGuid(),
                ParentId = parentId,
                FullName = childDto.FullName,
                DateOfBirth = childDto.DateOfBirth,
                Gender = childDto.Gender,
                MedicalHistory = childDto.MedicalHistory,
                BloodType = childDto.BloodType,
                HasChronicIllnesses = childDto.HasChronicIllnesses,
                ChronicIllnessesDescription = childDto.ChronicIllnessesDescription,
                HasAllergies = childDto.HasAllergies,
                AllergiesDescription = childDto.AllergiesDescription,
                HasRecentMedication = childDto.HasRecentMedication,
                RecentMedicationDescription = childDto.RecentMedicationDescription,
                HasOtherSpecialCondition = childDto.HasOtherSpecialCondition,
                OtherSpecialConditionDescription = childDto.OtherSpecialConditionDescription
            };

            // Save to database
            await _unitOfWork.ChildRepository.AddAsync(child);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"Parent {parentId} successfully created child profile {child.Id}");

            // Convert to ChildDto
            return new ChildDto
            {
                Id = child.Id,
                FullName = child.FullName,
                DateOfBirth = child.DateOfBirth,
                Gender = child.Gender,
                MedicalHistory = child.MedicalHistory,
                BloodType = child.BloodType,
                HasChronicIllnesses = child.HasChronicIllnesses,
                ChronicIllnessesDescription = child.ChronicIllnessesDescription,
                HasAllergies = child.HasAllergies,
                AllergiesDescription = child.AllergiesDescription,
                HasRecentMedication = child.HasRecentMedication,
                RecentMedicationDescription = child.RecentMedicationDescription,
                HasOtherSpecialCondition = child.HasOtherSpecialCondition,
                OtherSpecialConditionDescription = child.OtherSpecialConditionDescription
            };
        }
        catch (KeyNotFoundException ex)
        {
            _loggerService.Warn($"Warning: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while creating child profile: {ex.Message}");
            throw new Exception("An error occurred while creating the child profile. Please try again later.");
        }
    }
}