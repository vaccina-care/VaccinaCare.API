using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class CustomerController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IClaimsService _claimsService;
        

        public CustomerController(IUserService userService, IClaimsService claimsService)
        {
            _userService = userService;
            _claimsService = claimsService;
        }

        [HttpGet("users/me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                Guid currentUserId = _claimsService.GetCurrentUserId;
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
                var errorResult = ApiResult<object>.Error($"An error occurred while retrieving user profile: {ex.Message}");
                return StatusCode(500, errorResult);
            }
        }

        
        
    }
}