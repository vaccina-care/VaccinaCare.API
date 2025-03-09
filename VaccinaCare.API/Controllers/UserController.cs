using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public UserController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
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
            // Call the service layer to get all users
            var users = await _userService.GetAllUsersAsync();

            // Return the response wrapped in ApiResult
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
}