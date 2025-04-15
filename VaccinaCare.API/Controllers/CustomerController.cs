using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/users")]
public class CustomerController : ControllerBase
{
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _logger;
    private readonly IUserService _userService;

    public CustomerController(IUserService userService, IClaimsService claimsService, ILoggerService logger)
    {
        _userService = userService;
        _claimsService = claimsService;
        _logger = logger;
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var currentUserId = _claimsService.GetCurrentUserId;
            var currentUser = await _userService.GetUserDetails(currentUserId);

            var result = ApiResult<object>.Success(currentUser, "User profile retrieved successfully.");
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            var errorResult = ApiResult<object>.Error(ex.Message);
            return Unauthorized(errorResult);
        }
        catch (Exception ex)
        {
            var errorResult =
                ApiResult<object>.Error($"An error occurred while retrieving user profile: {ex.Message}");
            return StatusCode(500, errorResult);
        }
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateUserProfile([FromForm] UserUpdateDto userUpdateDto)
    {
        try
        {
            if (userUpdateDto == null)
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Request body cannot be empty.",
                    Data = null
                });

            var currentUserId = _claimsService.GetCurrentUserId;

            if (currentUserId == Guid.Empty)
                return Unauthorized(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Unauthorized request.",
                    Data = null
                });

            var updatedUser = await _userService.UpdateUserInfo(currentUserId, userUpdateDto);

            return Ok(new ApiResult<UserUpdateDto>
            {
                IsSuccess = true,
                Message = "User profile updated successfully.",
                Data = updatedUser
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An internal server error occurred.",
                Data = null
            });
        }
    }
}