using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.PolicyDTOs;
using VaccinaCare.Application.Ultils;
using Microsoft.AspNetCore.Authorization;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.API.Controllers;

[Route("api/policy")]
[ApiController]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;
    private readonly ILoggerService _logger;

    public PolicyController(IPolicyService policyService, ILoggerService logger)
    {
        _policyService = policyService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<PolicyDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> CreatePolicy([FromBody] PolicyDto policyDto)
    {
        try
        {
            _logger.Info("Received request to create policy.");

            if (policyDto == null || string.IsNullOrEmpty(policyDto.PolicyName))
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid policy data. Name is required."
                });

            var policy = await _policyService.CreatePolicyAsync(policyDto);
            _logger.Success("Policy created successfully.");

            return StatusCode(201, new ApiResult<PolicyDto>
            {
                IsSuccess = true,
                Message = "Policy created successfully.",
                Data = policy
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating policy: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while creating policy. Please try again later."
            });
        }
    }

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ApiResult<Pagination<PolicyDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllPolicies([FromQuery] PaginationParameter pagination)
    {
        try
        {
            _logger.Info("Received request to get policy list.");

            var policies = await _policyService.GetAllPolicyAsync(pagination);

            _logger.Success("Fetched policies successfully.");

            return Ok(new ApiResult<Pagination<PolicyDto>>
            {
                IsSuccess = true,
                Message = "Policy list retrieved successfully.",
                Data = policies
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching policies: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the policy list. Please try again later."
            });
        }
    }

    [HttpGet("{policyId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetPolicyById(Guid policyId)
    {
        try
        {
            _logger.Info($"Received request to get policy with ID: {policyId}");

            var policy = await _policyService.GetPolicyByIdAsync(policyId);

            if (policy == null)
            {
                _logger.Warn($"Policy {policyId} not found.");
                return NotFound(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Policy not found."
                });
            }

            return Ok(new ApiResult<PolicyDto>
            {
                IsSuccess = true,
                Message = "Policy retrieved successfully.",
                Data = policy
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving policy {policyId}: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving policy."
            });
        }
    }

    [HttpPut("{policyId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> UpdatePolicy(Guid policyId, [FromBody] PolicyDto policyDto)
    {
        try
        {
            _logger.Info($"Received request to update policy {policyId}.");

            if (policyDto == null || string.IsNullOrEmpty(policyDto.PolicyName))
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid policy data. Name is required."
                });

            var updatedPolicy = await _policyService.UpdatePolicyAsync(policyId, policyDto);
            _logger.Success($"Policy {policyId} updated successfully.");

            return Ok(new ApiResult<PolicyDto>
            {
                IsSuccess = true,
                Message = "Policy updated successfully.",
                Data = updatedPolicy
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating policy {policyId}: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while updating policy. Please try again later."
            });
        }
    }

    [HttpDelete("{policyId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeletePolicy(Guid policyId)
    {
        try
        {
            _logger.Info($"Received request to delete policy {policyId}.");
            await _policyService.DeletePolicyAsync(policyId);
            _logger.Success($"Policy {policyId} deleted successfully.");

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "Policy deleted successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting policy {policyId}: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while deleting policy. Please try again later."
            });
        }
    }
}
