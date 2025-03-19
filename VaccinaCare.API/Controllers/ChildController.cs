using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.ChildDTOs;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/children")]
public class ChildController : ControllerBase
{
    private readonly IChildService _childService;
    private readonly ILoggerService _logger;

    public ChildController(IChildService childService, ILoggerService logger)
    {
        _childService = childService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<ChildDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> AddChildrenInfo([FromBody] CreateChildDto childDto)
    {
        try
        {
            _logger.Info("Received request to create child profile.");

            if (childDto == null)
            {
                _logger.Warn("ChildDto is null.");
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid data. Child information is required."
                });
            }

            var child = await _childService.CreateChildAsync(childDto);

            _logger.Success($"Child profile {child.Id} created successfully.");

            return Ok(new ApiResult<ChildDto>
            {
                IsSuccess = true,
                Message = "Child profile created successfully.",
                Data = child
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Child creation failed: {ex.Message}");
            return NotFound(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while creating child profile: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while creating the child profile. Please try again later."
            });
        }
    }

    [HttpGet]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<List<ChildDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetChildrenByParent()
    {
        try
        {
            _logger.Info("Received request to get children list.");

            var children = await _childService.GetChildrenByParentAsync(); // Gọi service mà KHÔNG truyền pagination

            _logger.Success($"Fetched {children.Count} children successfully.");

            return Ok(new ApiResult<List<ChildDto>>
            {
                IsSuccess = true,
                Message = "Children list retrieved successfully.",
                Data = children
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching children: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the children list. Please try again later."
            });
        }
    }

    [HttpPut("{childId}")]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<ChildDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateChild(Guid childId, [FromBody] UpdateChildDto updateChildDto)
    {
        if (updateChildDto == null)
            return BadRequest(new ApiResult<object> { IsSuccess = false, Message = "Invalid child data." });

        try
        {
            var result = await _childService.UpdateChildrenAsync(childId, updateChildDto);
            return Ok(new ApiResult<ChildDto>
                { IsSuccess = true, Message = "Child profile updated successfully.", Data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResult<object> { IsSuccess = false, Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500,
                new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating the child profile. Please try again later."
                });
        }
    }

    [HttpDelete("{childId}")]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> DeleteChild(Guid childId)
    {
        try
        {
            await _childService.DeleteChildrenByParentIdAsync(childId);
            return Ok(new ApiResult<object> { IsSuccess = true, Message = "Child profile deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResult<object> { IsSuccess = false, Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500,
                new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting the child profile. Please try again later."
                });
        }
    }
}