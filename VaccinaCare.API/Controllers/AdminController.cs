using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using Microsoft.AspNetCore.Authorization;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Application.Interface.Common;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly ILoggerService _logger;

    public AdminController(IUserService userService, IAuthService authService,ILoggerService loggerService)
    {
        _userService = userService;
        _authService = authService;
        _logger = loggerService;
    }

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            _logger.Info("Fetched all user....");

            var users = await _userService.GetAllUsersForAdminAsync();

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Data = users,
                Message = "Fetched all users successfully."
            });
        }
        catch (Exception ex)
        {
            // Log and return error response
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Data = null,
                Message = $"An error occurred: {ex.Message}"
            });
        }
    }
    [HttpPost("create-staff")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> CreateStaff([FromBody] StaffDTO staffDTO)
    {
        _logger.Info("Creating attempt initiated.");

        if (staffDTO == null || string.IsNullOrWhiteSpace(staffDTO.Email) ||
            string.IsNullOrWhiteSpace(staffDTO.Password))
        {
            _logger.Warn("Invalid create request. Email and password are required.");
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
        }

        try
        {
            var user = await _userService.CreateStaffAsync(staffDTO);

            if (user == null)
            {
                _logger.Warn($"Creating failed for email: {staffDTO.Email}. Email might already be in use.");
                return BadRequest(ApiResult<object>.Error("Creating failed. Email might already be in use."));
            }

            _logger.Success($"User {staffDTO.Email} registered successfully.");         

            var staff = new StaffDTO
            {
                Email = user.Email,
                FullName = user.FullName
            };

            return Ok(ApiResult<StaffDTO>.Success(staff, "Registration successful."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during creating staff: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creating staff."));
        }
    }
    [HttpPut]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateUserInfo(Guid userId, [FromBody] UserUpdateDto userUpdateDto)
    {
        _logger.Info($"[UpdateUserInfo] Start updating user info for UserId: {userId}");

        if (!ModelState.IsValid)
        {
            _logger.Warn($"[UpdateUserInfo] Invalid request data for UserId: {userId}");
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "Invalid request data",
                Data = ModelState
            });
        }

        try
        {
            var updatedUser = await _userService.UpdateUserInfo(userId, userUpdateDto);
            _logger.Info($"[UpdateUserInfo] Successfully updated user info for UserId: {userId}");

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "User info updated successfully",
                Data = updatedUser
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"[UpdateUserInfo] UserId {userId} not found: {ex.Message}");
            return NotFound(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"[UpdateUserInfo] Error updating user info for UserId {userId}: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "Internal server error"
            });
        }
    }
    [HttpDelete]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.Info($"Delete attempt initiated for user ID: {id}.");

        if (id == Guid.Empty)
        {
            _logger.Warn("Invalid delete request. User ID is required.");
            return BadRequest(ApiResult<object>.Error("400 - Invalid delete request. User ID is required."));
        }

        try
        {
            var result = await _userService.DeleteUserAsync(id);

            if (!result)
            {
                _logger.Warn($"Delete failed. No user found with ID: {id}.");
                return BadRequest(ApiResult<object>.Error("Delete failed. No user found with the provided ID."));
            }

            _logger.Success($"User {id} deleted successfully.");
            return Ok(ApiResult<object>.Success(null, "User deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during deleting user: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during deleting user."));
        }
    }
}

