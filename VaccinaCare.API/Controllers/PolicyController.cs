﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.PolicyDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.API.Controllers;

[Route("api/policies")]
[ApiController]
public class PolicyController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IPolicyService _policyService;

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
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyDto policyDto)
    {
        try
        {
            if (policyDto == null || string.IsNullOrEmpty(policyDto.PolicyName))
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid policy data. Name is required."
                });

            var policy = await _policyService.CreatePolicyAsync(policyDto);

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
    [ProducesResponseType(typeof(ApiResult<Pagination<PolicyDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllPolicies([FromQuery] PaginationParameter pagination,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var policies = await _policyService.GetAllPolicyAsync(pagination, searchTerm);

            return Ok(ApiResult<object>.Success(new
            {
                totalCount = policies.TotalCount, policies
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("{policyId}")]
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
    public async Task<IActionResult> UpdatePolicy(Guid policyId, [FromBody] UpdatePolicyDto policyDto)
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