﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILoggerService _logger;
    private readonly IUserService _userService;

    public AdminController(IUserService userService, IAuthService authService, ILoggerService loggerService)
    {
        _userService = userService;
        _authService = authService;
        _logger = loggerService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResult<Pagination<UserDto>>), 200)]
    public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParameter paginationParameter,
        [FromQuery] string? searchTerm)
    {
        try
        {
            var users = await _userService.GetAllUsersForAdminAsync(paginationParameter, searchTerm);
            return Ok(ApiResult<object>.Success(new
            {
                totalCount = users.TotalCount, users
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpPost("users/staff")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto createStaffDto)
    {
        if (createStaffDto == null || string.IsNullOrWhiteSpace(createStaffDto.Email) ||
            string.IsNullOrWhiteSpace(createStaffDto.Password))
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));

        try
        {
            var user = await _userService.CreateStaffAsync(createStaffDto);
            if (user == null)
                return BadRequest(ApiResult<object>.Error("Creating failed. Email might already be in use."));
            var staff = new CreateStaffDto
            {
                Email = user.Email,
                FullName = user.FullName
            };

            return Ok(ApiResult<object>.Success(staff, "Registration successful."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creating staff."));
        }
    }

    [HttpPut("users/{userId}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> UpdateUserInfoByAdmin(Guid userId,
        [FromBody] UserUpdateDtoByAdmin userUpdateByAdminDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResult<object>.Error("Invalid request data"));
        try
        {
            var updatedUser = await _userService.UpdateUserInfoByAdmin(userId, userUpdateByAdminDto);

            return Ok(ApiResult<object>.Success(updatedUser, "User info updated successfully by admin"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("Internal server error"));
        }
    }

    [HttpDelete("users/{userId}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest(ApiResult<object>.Error("400 - Invalid delete request. User ID is required."));

        try
        {
            var result = await _userService.DeactivateUserAsync(userId);

            if (!result)
                return BadRequest(ApiResult<object>.Error("Delete failed. No user found with the provided ID."));

            return Ok(ApiResult<object>.Success(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during deleting user."));
        }
    }
}