using Microsoft.AspNetCore.Identity.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class UserService : IUserService
{
    private readonly ILoggerService _logger;
    private readonly IBlobService _blobService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IEmailService _emailService;

    public UserService(ILoggerService logger, IUnitOfWork unitOfWork, IBlobService blobService,
        IClaimsService claimsService, IEmailService emailService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _blobService = blobService;
        _claimsService = claimsService;
        _emailService = emailService;
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
    public async Task<Pagination<UserDto>> GetAllUsersForAdminAsync(PaginationParameter paginationParameter,
        string? searchTerm = null)
    {
        try
        {
            _logger.Info("Fetching users from the database with pagination and search.");

            var query = _unitOfWork.UserRepository.GetQueryable();

            // Apply search by fullName, email, or roleName if searchTerm is not empty
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();

                // Fetch users first, and then filter by role name using ToString()
                var users = await query
                    .Where(u => u.IsDeleted == false) // Assuming IsDeleted field exists for filtering
                    .ToListAsync(); // Fetch the data into memory

                users = users.Where(u =>
                        u.FullName.ToLower().Contains(searchLower) ||
                        u.Email.ToLower().Contains(searchLower) ||
                        u.RoleName.ToString().ToLower().Contains(searchLower)) // Perform the ToString() here
                    .ToList();

                var totalUsers = users.Count;

                var userDtos = users
                    .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                    .Take(paginationParameter.PageSize)
                    .Select(u => new UserDto
                    {
                        UserId = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        RoleName = u.RoleName, // Store RoleName as string in the DTO
                        CreatedAt = u.CreatedAt
                    }).ToList();

                _logger.Info(
                    $"Successfully fetched {userDtos.Count} users out of {totalUsers} (searchTerm: {searchTerm}).");

                return new Pagination<UserDto>(userDtos, totalUsers, paginationParameter.PageIndex,
                    paginationParameter.PageSize);
            }

            // In case there's no search term, return all users with pagination
            else
            {
                var totalUsers = await query.CountAsync();

                var users = await query
                    .Where(u => u.IsDeleted == false)
                    .OrderBy(u => u.CreatedAt)
                    .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                    .Take(paginationParameter.PageSize)
                    .ToListAsync();

                var userDtos = users.Select(u => new UserDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.RoleName,
                    CreatedAt = u.CreatedAt
                }).ToList();

                return new Pagination<UserDto>(userDtos, totalUsers, paginationParameter.PageIndex,
                    paginationParameter.PageSize);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while fetching users: {ex.Message}");
            throw;
        }
    }


    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        try
        {
            _logger.Info($"Attempting to deactivate user with ID: {userId}");

            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.Warn($"User with ID {userId} not found.");
                return false;
            }

            // Prevent deactivation for users with 'admin' role
            if (user.RoleName == RoleType.Admin) // Assuming 'RoleName' is stored as a string
            {
                _logger.Warn($"User with ID {userId} is an admin and cannot be deactivated.");
                return false;
            }

            if (user.IsDeleted)
            {
                _logger.Warn($"User with ID {userId} is already deactivated.");
                return false;
            }

            // Update the user status to soft-deleted
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = _claimsService.GetCurrentUserId;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.UserRepository.SoftRemove(user);
            await _unitOfWork.SaveChangesAsync();

            // Send deactivation notification email
            var emailRequest = new EmailRequestDTO
            {
                UserEmail = user.Email,
                UserName = user.FullName
            };
            await _emailService.SendDeactivationNotificationAsync(emailRequest);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while deactivating user with ID {userId}: {ex.Message}");
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