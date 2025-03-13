using Microsoft.AspNetCore.Identity.Data;
using System.Text.RegularExpressions;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class UserService : IUserService
{
    private readonly ILoggerService _logger;
    private readonly IBlobService _blobService;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(ILoggerService logger, IUnitOfWork unitOfWork, IBlobService blobService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _blobService = blobService;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        try
        {
            // Log the start of the operation
            _logger.Info("Fetching all users from the database.");

            // Fetch all users from the repository
            var users = await _unitOfWork.UserRepository.GetAllAsync();

            // Log the success of the operation
            _logger.Info($"Successfully fetched {users.Count()} users.");

            return users;
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.Error($"An error occurred while fetching users: {ex.Message}");
            throw;
        }
    }

    public async Task<CurrentUserDTO> GetUserDetails(Guid id)
    {
        if (id == Guid.Empty)
        {
            _logger.Warn("Attempted to fetch user with an empty GUID.");
            throw new ArgumentException("Invalid user ID.");
        }

        try
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);

            if (user == null)
            {
                _logger.Warn($"No user found with ID: {id}");
                throw new KeyNotFoundException($"User with ID {id} not found.");
            }

            _logger.Info($"Successfully fetched user with ID: {id}.");

            return new CurrentUserDTO
            {
                FullName = user.FullName,
                Email = user.Email,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                RoleName = user.RoleName,
                ImageUrl = user.ImageUrl,
                DateOfBirth = user.DateOfBirth
            };
        }
        catch (KeyNotFoundException knfEx)
        {
            _logger.Error($"User retrieval error: {knfEx.Message}");
            throw;
        }
        catch (ArgumentException argEx)
        {
            _logger.Error($"Invalid argument: {argEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"An unexpected error occurred while fetching user details for ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<UserUpdateDto> UpdateUserInfo(Guid userId, UserUpdateDto userUpdateDto)
    {
        try
        {
            _logger.Info($"Starting user info update for UserId: {userId}");

            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.Warn($"User with ID {userId} not found.");
                throw new KeyNotFoundException("User not found.");
            }

            var isUpdated = false;

            if (!string.IsNullOrEmpty(userUpdateDto.FullName) && user.FullName != userUpdateDto.FullName)
            {
                user.FullName = userUpdateDto.FullName;
                isUpdated = true;
            }

            if (userUpdateDto.Gender.HasValue && user.Gender != userUpdateDto.Gender)
            {
                user.Gender = userUpdateDto.Gender;
                isUpdated = true;
            }

            if (userUpdateDto.DateOfBirth.HasValue && user.DateOfBirth != userUpdateDto.DateOfBirth)
            {
                user.DateOfBirth = userUpdateDto.DateOfBirth;
                isUpdated = true;
            }

            if (userUpdateDto.ImageFile != null && userUpdateDto.ImageFile.Length > 0)
            {
                using var stream = userUpdateDto.ImageFile.OpenReadStream();
                var fileName = $"profile_pictures/{userId}_{userUpdateDto.ImageFile.FileName}";

                await _blobService.UploadFileAsync(fileName, stream);
                var previewUrl = await _blobService.GetPreviewUrlAsync(fileName);

                user.ImageUrl = previewUrl;
                isUpdated = true;
            }

            if (!string.IsNullOrEmpty(userUpdateDto.PhoneNumber) && user.PhoneNumber != userUpdateDto.PhoneNumber)
            {
                user.PhoneNumber = userUpdateDto.PhoneNumber;
                isUpdated = true;
            }

            if (!string.IsNullOrEmpty(userUpdateDto.Address) && user.Address != userUpdateDto.Address)
            {
                user.Address = userUpdateDto.Address;
                isUpdated = true;
            }

            if (!string.IsNullOrEmpty(userUpdateDto.PhoneNumber) &&
                user.PhoneNumber != userUpdateDto.PhoneNumber)
            {
                if (!Regex.IsMatch(userUpdateDto.PhoneNumber, @"^\d{10,15}$"))
                    throw new ArgumentException("Invalid phone number format.");
                user.PhoneNumber = userUpdateDto.PhoneNumber;
                isUpdated = true;
            }

            if (!isUpdated)
            {
                _logger.Warn($"No changes detected for UserId: {userId}");
                return new UserUpdateDto
                {
                    FullName = user.FullName,
                    Gender = user.Gender,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address,
                    ImageUrl = user.ImageUrl,
                    PhoneNumber = user.PhoneNumber
                };
            }

            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User info updated successfully for UserId: {userId}");

            return new UserUpdateDto
            {
                FullName = user.FullName,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                ImageUrl = user.ImageUrl, // Trả về link preview ảnh
                PhoneNumber = user.PhoneNumber
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating user info for UserId: {userId}. Exception: {ex.Message}");
            throw;
        }
    }

    //admin methods: 
    public async Task<IEnumerable<GetUserDTO>> GetAllUsersForAdminAsync()
    {
        try
        {
            _logger.Info("Fetching all users from the database.");

            var users = await _unitOfWork.UserRepository.GetAllAsync();

            var userDtos = users.Select(u => new GetUserDTO
            {
                FullName = u.FullName,
                Email = u.Email,
                RoleName = u.RoleName,
                CreatedAt = u.CreatedAt
            });

            _logger.Info($"Successfully fetched {userDtos.Count()} user.");

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while fetching users: {ex.Message}");
            throw;
        }
    }

    //email when deactivate successfully
    public async Task<bool> DeactivateUserAsync(Guid id)
    {
        try
        {
            _logger.Info($"Attempting to delete user with ID: {id}");

            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (user == null)
            {
                _logger.Warn($"User with ID {id} not found.");
                return false;
            }

            if (user.IsDeleted)
            {
                _logger.Warn($"User with ID {id} is already deleted.");
                return false;
            }

            await _unitOfWork.UserRepository.SoftRemove(user);
            await _unitOfWork.SaveChangesAsync();


            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while deleting user: {ex.Message}");
            throw;
        }
    }

    public async Task<User> CreateStaffAsync(CreateStaffDto createStaffDto)
    {
        try
        {
            _logger.Info("Starting creating staff account.");

            if (string.IsNullOrWhiteSpace(createStaffDto.Email) || string.IsNullOrWhiteSpace(createStaffDto.Password))
            {
                _logger.Warn("Email or Password is missing in the registration request.");
                return null;
            }

            var existingStaff =
                await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == createStaffDto.Email);
            if (existingStaff != null)
            {
                _logger.Warn($"Creating attempt failed. Email {createStaffDto.Email} is already in use.");
                return null;
            }

            //Hash password
            _logger.Info("Hashing the password.");
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword(createStaffDto.Password);

            //Creating new staff
            _logger.Info("Creating new staff");
            var newStaff = new User
            {
                Email = createStaffDto.Email,
                FullName = createStaffDto.FullName,
                PasswordHash = hashedPassword,
                RoleName = RoleType.Staff
            };

            await _unitOfWork.UserRepository.AddAsync(newStaff);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User {createStaffDto.Email} successfully registered.");
            return newStaff;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"An unexpected error occurred during registration for email {createStaffDto?.Email ?? "Unknown"}: {ex.Message}");
            return null;
        }
    }
}