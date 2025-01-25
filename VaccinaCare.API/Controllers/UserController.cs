using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.UserDTOs;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("profile")]
        [Authorize(Policy = "CustomerPolicy")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<ObjectResult> GetUserProfile()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserDetailsAsync(User);

                var result = ApiResult<CurrentUserDTO>.Success(currentUser, "User profile retrieved successfully.");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorResult = ApiResult<object>.Error(ex.Message);
                return Unauthorized(errorResult);
            }
            catch (Exception ex)
            {
                var errorResult = ApiResult<object>.Error($"An error occurred while retrieving user profile{ex}");
                return StatusCode(500, errorResult);
            }
        }
    }
}