using Microsoft.EntityFrameworkCore;
using Minio.Helper;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class ChildService : IChildService
{
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _loggerService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVaccineSuggestionService _vaccineSuggestionService;

    public ChildService(ILoggerService loggerService, IUnitOfWork unitOfWork, IClaimsService claimsService,
        IVaccineSuggestionService vaccineSuggestionService, INotificationService notificationService)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _vaccineSuggestionService = vaccineSuggestionService;
        _notificationService = notificationService;
    }

    /// <summary>
    ///     Cho phép Parent tạo thông tin của trẻ em
    /// </summary>
    /// <param name="childDto"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<ChildDto> CreateChildAsync(CreateChildDto childDto)
    {
        try
        {
            // Get ParentId from ClaimsService
            var parentId = _claimsService.GetCurrentUserId;

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

            var notificationDTO = new NotificationForUserDTO
            {
                Title = "Child Profile Created!",
                Content = "Your child's profile has been successfully created.",
                Url = "",
                UserId = parentId
            };
            await _notificationService.PushNotificationWhenUserUseService(parentId, notificationDTO);
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

    /// <summary>
    ///     GET tất cả thông tin của trẻ em thông qua Id của Parent
    /// </summary>
    /// <param name="pagination"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<ChildDto>> GetChildrenByParentAsync()
    {
        try
        {
            // Lấy ParentId từ ClaimsService
            var parentId = _claimsService.GetCurrentUserId;
            _loggerService.Info($"Fetching all children for parent {parentId}");

            // Truy vấn danh sách trẻ em thuộc về phụ huynh
            var children = await _unitOfWork.ChildRepository.GetQueryable()
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.FullName) // Sắp xếp theo tên (nếu cần)
                .ToListAsync();

            if (!children.Any())
            {
                _loggerService.Warn($"No children found for parent {parentId}.");
                return new List<ChildDto>(); // Trả về danh sách rỗng thay vì null
            }

            _loggerService.Success($"Retrieved {children.Count} children for parent {parentId}");

            // Convert sang danh sách ChildDto
            return children.Select(child => new ChildDto
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
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while fetching children: {ex.Message}");
            throw new Exception("An error occurred while fetching children. Please try again later.");
        }
    }

    /// <summary>
    ///     Soft delete 1 trẻ em thuộc về parent dựa trên parent id
    /// </summary>
    /// <param name="childId"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task DeleteChildrenByParentIdAsync(Guid childId)
    {
        try
        {
            // Lấy ParentId
            var parentId = _claimsService.GetCurrentUserId;
            _loggerService.Info($"Parent {parentId} requested to delete child {childId}");

            // Lấy thông tin trẻ em từ DB
            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);

            // Kiểm tra xem child có tồn tại và có thuộc về parent hay không
            if (child == null || child.ParentId != parentId)
            {
                _loggerService.Warn($"Child {childId} not found or does not belong to parent {parentId}.");
                throw new KeyNotFoundException("Child not found or access denied.");
            }

            // Thực hiện Soft Delete
            await _unitOfWork.ChildRepository.SoftRemove(child);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"Parent {parentId} successfully deleted child {childId}.");
        }
        catch (KeyNotFoundException ex)
        {
            _loggerService.Warn($"Warning: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error deleting child profile {childId}: {ex.Message}");
            throw new Exception("An error occurred while deleting the child profile. Please try again later.");
        }
    }

    /// <summary>
    ///     Update thông tin của children, field nào có nhập thì update, không nhập thì để nguyên
    /// </summary>
    /// <param name="childId"></param>
    /// <param name="childDto"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<ChildDto> UpdateChildrenAsync(Guid childId, UpdateChildDto childDto)
    {
        try
        {
            var parentId = _claimsService.GetCurrentUserId;
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

            #region Cập nhật các trường chỉ nếu chúng không phải là null

            if (childDto.FullName != null)
                child.FullName = childDto.FullName;

            if (childDto.DateOfBirth != null)
                child.DateOfBirth = childDto.DateOfBirth;

            if (childDto.Gender != null)
                child.Gender = childDto.Gender;

            if (childDto.MedicalHistory != null)
                child.MedicalHistory = childDto.MedicalHistory;

            if (childDto.BloodType != null)
                child.BloodType = childDto.BloodType;

            if (childDto.HasChronicIllnesses != null)
                child.HasChronicIllnesses = childDto.HasChronicIllnesses;

            if (childDto.ChronicIllnessesDescription != null)
                child.ChronicIllnessesDescription = childDto.ChronicIllnessesDescription;

            if (childDto.HasAllergies != null)
                child.HasAllergies = childDto.HasAllergies;

            if (childDto.AllergiesDescription != null)
                child.AllergiesDescription = childDto.AllergiesDescription;

            if (childDto.HasRecentMedication != null)
                child.HasRecentMedication = childDto.HasRecentMedication;

            if (childDto.RecentMedicationDescription != null)
                child.RecentMedicationDescription = childDto.RecentMedicationDescription;

            if (childDto.HasOtherSpecialCondition != null)
                child.HasOtherSpecialCondition = childDto.HasOtherSpecialCondition;

            if (childDto.OtherSpecialConditionDescription != null)
                child.OtherSpecialConditionDescription = childDto.OtherSpecialConditionDescription;

            #endregion Cập nhật các trường chỉ nếu chúng không phải là null

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

    public async Task<int> GetChildrenProfile()
    {
        try
        {
            _loggerService.Info("Fetching children profile....");

            var totalChildrenProfile = await _unitOfWork.ChildRepository.GetAllAsync();
            var count = totalChildrenProfile.Count(c => c.IsDeleted == false);

            _loggerService.Info($"Successfully retrieved vaccine count : {count}");
            return count;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error occured while getting children profile count: {ex.Message}");
            return 0;   
        }
    }
}