using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class ChildService : IChildService
{
    private readonly ILoggerService _loggerService;
    private readonly IClaimsService _claimsService;
    private readonly IVaccineSuggestionService _vaccineSuggestionService;
    private readonly IUnitOfWork _unitOfWork;

    public ChildService(ILoggerService loggerService, IUnitOfWork unitOfWork, IClaimsService claimsService, IVaccineSuggestionService vaccineSuggestionService)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _vaccineSuggestionService = vaccineSuggestionService;
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

    public async Task DeleteChildAsync(Guid childId)
    {
        try
        {
            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (child == null)
            {
                _loggerService.Warn($"Child with ID {childId} not found.");
                throw new KeyNotFoundException("Child not found.");
            }

            await _unitOfWork.ChildRepository.SoftRemove(child);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success($"Child profile {childId} deleted successfully.");
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error deleting child profile {childId}: {ex.Message}");
            throw;
        }
    }

    public async Task<Pagination<ChildDto>> GetChildrenByParentAsync(PaginationParameter pagination)
    {
        try
        {
            // Get ParentId from ClaimsService
            Guid parentId = _claimsService.GetCurrentUserId;

            _loggerService.Info(
                $"Fetching children for parent {parentId} with pagination: Page {pagination.PageIndex}, Size {pagination.PageSize}");

            // Retrieve queryable list of children
            var query = _unitOfWork.ChildRepository.GetQueryable()
                .Where(c => c.ParentId == parentId);

            // Get total count before applying pagination
            int totalChildren = await query.CountAsync();

            // Apply pagination
            var children = await query
                .OrderBy(c => c.FullName) // Sorting by name, modify if needed
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            if (!children.Any())
            {
                _loggerService.Warn($"No children found for parent {parentId} on page {pagination.PageIndex}.");
                return new Pagination<ChildDto>(new List<ChildDto>(), 0, pagination.PageIndex, pagination.PageSize);
            }

            _loggerService.Success(
                $"Retrieved {children.Count} children for parent {parentId} on page {pagination.PageIndex}");

            // Convert to ChildDto list
            var childDtos = children.Select(child => new ChildDto
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
            }).ToList();

            return new Pagination<ChildDto>(childDtos, totalChildren, pagination.PageIndex, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while fetching children: {ex.Message}");
            throw new Exception("An error occurred while fetching children. Please try again later.");
        }
    }

    public async Task<ChildDto> UpdateChildAsync(Guid childId, UpdateChildDto childDto)
    {
        try
        {
            Guid parentId = _claimsService.GetCurrentUserId;
            _loggerService.Info($"Starting child profile update for parent {parentId}");

            var parent = await _unitOfWork.UserRepository.GetByIdAsync(parentId);
            if (parent == null)
            {
                _loggerService.Warn($"Parent {parentId} does not exist.");
                throw new KeyNotFoundException("Parent not found.");
            }

            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (child == null || child.ParentId != parentId)
            {
                _loggerService.Warn($"Child {childId} not found or does not belong to parent {parentId}");
                throw new KeyNotFoundException("Child not found or access denied.");
            }

            child.FullName = childDto.FullName ?? child.FullName;
            child.DateOfBirth = childDto.DateOfBirth ?? child.DateOfBirth;
            child.Gender = childDto.Gender ?? child.Gender;
            child.MedicalHistory = childDto.MedicalHistory ?? child.MedicalHistory;
            child.BloodType = childDto.BloodType ?? child.BloodType;
            child.HasChronicIllnesses = childDto.HasChronicIllnesses ?? child.HasChronicIllnesses;
            child.ChronicIllnessesDescription = childDto.ChronicIllnessesDescription ?? child.ChronicIllnessesDescription;
            child.HasAllergies = childDto.HasAllergies ?? child.HasAllergies;
            child.AllergiesDescription = childDto.AllergiesDescription ?? child.AllergiesDescription;
            child.HasRecentMedication = childDto.HasRecentMedication ?? child.HasRecentMedication;
            child.RecentMedicationDescription = childDto.RecentMedicationDescription ?? child.RecentMedicationDescription;
            child.HasOtherSpecialCondition = childDto.HasOtherSpecialCondition ?? child.HasOtherSpecialCondition;
            child.OtherSpecialConditionDescription = childDto.OtherSpecialConditionDescription ?? child.OtherSpecialConditionDescription;

            await _unitOfWork.ChildRepository.Update(child);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"Parent {parentId} successfully updated child profile {childId}");

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
            _loggerService.Error($"Error while updating child profile: {ex.Message}");
            throw new Exception("An error occurred while updating the child profile. Please try again later.");
        }
    }
}