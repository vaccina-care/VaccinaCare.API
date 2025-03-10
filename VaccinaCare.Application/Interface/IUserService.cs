﻿using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<CurrentUserDTO> GetUserDetails(Guid id);
    Task<UserUpdateDto> UpdateUserInfo(Guid userId, UserUpdateDto userUpdateDto);
}