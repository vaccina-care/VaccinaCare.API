using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        // Endpoint to get the profile of the authenticated user
        [HttpGet("profile")]
        [Authorize(Policy = "CustomerPolicy")] // Requires Customer role
        public IActionResult GetUserProfile()
        {
            // Retrieve claims from the authenticated user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { Message = "User is not properly authenticated." });
            }

            // Return the user's profile information
            return Ok(new
            {
                Message = "User profile retrieved successfully.",
                UserId = userId,
                Email = email,
                Role = role
            });
        }
    }
}
