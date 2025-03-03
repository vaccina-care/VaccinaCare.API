using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

namespace VaccinaCare.API.Controllers;

using Humanizer;
using Application.Interface.Common;
using Application.Ultils;

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

            var createdRule = await _vaccineIntervalRulesService.CreateVaccineIntervalRuleAsync(
                rulesDTO.VaccineId, rulesDTO.RelatedVaccineId, rulesDTO.MinIntervalDays, rulesDTO.CanBeGivenTogether);

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
}