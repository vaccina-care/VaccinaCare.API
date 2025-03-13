using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IUserService
{
    Task<CurrentUserDTO> GetUserDetails(Guid id);
    Task<UserUpdateDto> UpdateUserInfo(Guid userId, UserUpdateDto userUpdateDto);

    //admin
    Task<bool> DeactivateUserAsync(Guid id);
    Task<User> CreateStaffAsync(CreateStaffDto createStaffDto);
    Task<IEnumerable<GetUserDTO>> GetAllUsersForAdminAsync();
}