using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
    public class UserService : IUserService
    {
        private readonly ILoggerService _logger;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(ILoggerService logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
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

                bool isUpdated = false;

                if (!string.IsNullOrEmpty(userUpdateDto.FullName))
                {
                    user.FullName = userUpdateDto.FullName;
                    isUpdated = true;
                }

                if (userUpdateDto.Gender.HasValue)
                {
                    user.Gender = userUpdateDto.Gender;
                    isUpdated = true;
                }

                if (userUpdateDto.DateOfBirth.HasValue)
                {
                    user.DateOfBirth = userUpdateDto.DateOfBirth;
                    isUpdated = true;
                }

                if (!string.IsNullOrEmpty(userUpdateDto.ImageUrl))
                {
                    user.ImageUrl = userUpdateDto.ImageUrl;
                    isUpdated = true;
                }

                if (!string.IsNullOrEmpty(userUpdateDto.PhoneNumber))
                {
                    user.PhoneNumber = userUpdateDto.PhoneNumber;
                    isUpdated = true;
                }

                
                if (!string.IsNullOrEmpty(userUpdateDto.Address))
                {
                    user.Address = userUpdateDto.Address;
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
                    ImageUrl = user.ImageUrl,
                    PhoneNumber = user.PhoneNumber
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating user info for UserId: {userId}. Exception: {ex.Message}");
                throw;
            }
        }
        
        
    }
}