using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

namespace VaccinaCare.API.Controllers;

using Humanizer;
using Application.Interface.Common;
using Application.Ultils;
using Application.Service;
using System.Diagnostics.Contracts;

[ApiController]
[Route("api/interval-rules")]
public class VaccineIntervalRulesController : ControllerBase
{
    private readonly IVaccineIntervalRulesService _vaccineIntervalRulesService;
    private readonly ILoggerService _logger;

    public VaccineIntervalRulesController(IVaccineIntervalRulesService vaccineIntervalRulesService,
        ILoggerService logger)
    {
        _vaccineIntervalRulesService = vaccineIntervalRulesService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> CreateVaccineIntervalRule([FromBody] VaccineIntervalRulesDTO rulesDTO)
    {
        if (rulesDTO == null) return Ok(ApiResult<object>.Error("400 - Invalid data."));

        try
        {
            var createdRule = await _vaccineIntervalRulesService.CreateVaccineIntervalRuleAsync(rulesDTO);
            if (createdRule == null)
                return Ok(ApiResult<object>.Error("400 - Rule creation failed. Please check input data."));

            return Ok(ApiResult<VaccineIntervalRulesDTO>.Success(createdRule, "Rule created successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllVaccineIntervalRule()
    {
        try
        {
            var vaccineIntervalRules = await _vaccineIntervalRulesService.GetAllVaccineIntervalRulesAsync();

            if (vaccineIntervalRules == null || vaccineIntervalRules.Count == 0)
                return Ok(ApiResult<object>.Error("No vaccine interval rules found."));

            return Ok(ApiResult<object>.Success(vaccineIntervalRules));
        }
        catch (Exception ex)
        {
            // Log the exception (optional)
            return StatusCode(500, ApiResult<object>.Error("An error occurred while retrieving data."));
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateVaccineIntervalRule(Guid id, [FromBody] VaccineIntervalRulesDTO updateDto)
    {
        try
        {
            var isUpdated = await _vaccineIntervalRulesService.UpdateVaccineIntervalRuleAsync(id, updateDto);
            if (isUpdated == null) return Ok(ApiResult<object>.Error("404 - Vaccine interval rule not found."));

            return Ok(ApiResult<object>.Success(null, "Vaccine interval rule updated successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("500 - Internal server error."));
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> DeleteVaccineIntervalRule(Guid id)
    {
        try
        {
            var isDeleted = await _vaccineIntervalRulesService.DeleteVaccineIntervalRuleAsync(id);
            if (!isDeleted) return Ok(ApiResult<object>.Error("404 - Vaccine interval rule not found."));

            return Ok(ApiResult<object>.Success(null, "Vaccine interval rule deleted successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("500 - Internal server error."));
        }
    }
}