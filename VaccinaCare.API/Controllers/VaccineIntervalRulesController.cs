using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

namespace VaccinaCare.API.Controllers;

using Humanizer;
using Application.Interface.Common;
using Application.Ultils;
using VaccinaCare.Application.Service;
using System.Diagnostics.Contracts;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> CreateVaccineIntervalRule([FromBody] VaccineIntervalRulesDTO rulesDTO)
    {
        _logger.Info("Create vaccine interval rule received.");
        if (rulesDTO == null)
        {
            _logger.Warn("CreateVaccineIntervalRule: Data is null");
            return BadRequest(ApiResult<object>.Error("400 - Invalid data."));
        }

        try
        {
            _logger.Info($"Attempting to create vaccine interval rule for VaccineId: {rulesDTO.VaccineId}");

            var createdRule = await _vaccineIntervalRulesService.CreateVaccineIntervalRuleAsync(rulesDTO);

            if (createdRule == null)
            {
                _logger.Warn("CreateVaccineIntervalRule: Rule creation failed due to validation issues.");
                return BadRequest(ApiResult<object>.Error("400 - Rule creation failed. Please check input data."));
            }

            _logger.Success(
                $"CreateVaccineIntervalRule: Rule created successfully for VaccineId: {rulesDTO.VaccineId}");
            return Ok(ApiResult<VaccineIntervalRulesDTO>.Success(createdRule, "Rule created successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during rule creation: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetAllVaccineIntervalRule()
    {
        try
        {
            var vaccineIntervalRules = await _vaccineIntervalRulesService.GetAllVaccineIntervalRulesAsync();
            if (vaccineIntervalRules == null || vaccineIntervalRules.Count == 0)
                return NotFound(ApiResult<object>.Error("404 - No vaccine interval rules available."));

            return Ok((ApiResult<List<VaccineIntervalRulesDTO>>.Success(vaccineIntervalRules, "Vaccine interval rules retrieved successfully.")));
        }
        catch(Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexepected error occurred while retrieving vaccine interval rules."));
        }
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVacicneIntervalRule(Guid id, [FromBody] VaccineIntervalRulesDTO updateDto)
    {
        try
        {
            var isUpdated = await _vaccineIntervalRulesService.UpdateVaccineIntervalRuleAsync(id, updateDto);
            if (isUpdated == null) return NotFound(ApiResult<object>.Error("404 - Vaccine interval rule not found."));

            return Ok(ApiResult<object>.Success(null, "Vaccine interval rule updated successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("500 - Internal server error."));
        }
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVaccineIntervalRule(Guid id)
    {
        try
        {
            var isDeleted = await _vaccineIntervalRulesService.DeleteVaccineIntervalRuleAsync(id);
            if (!isDeleted) return NotFound(ApiResult<object>.Error("404 - Vaccine interval rule  not found."));

            return Ok(ApiResult<object>.Success(null, "Vaccine interval rule deleted successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("500 - Internal server error."));
        }
    }
}