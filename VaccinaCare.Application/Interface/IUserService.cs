using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface;

public interface IUserService
{
    Task<CurrentUserDTO> GetUserDetails(Guid id);
    Task<UserUpdateDto> UpdateUserInfo(Guid userId, UserUpdateDto userUpdateDto);

    //admin
    Task<bool> DeactivateUserAsync(Guid userId);
    Task<User> CreateStaffAsync(CreateStaffDto createStaffDto);

    Task<Pagination<UserDto>> GetAllUsersForAdminAsync(PaginationParameter paginationParameter,
        string? searchTerm = null);
}