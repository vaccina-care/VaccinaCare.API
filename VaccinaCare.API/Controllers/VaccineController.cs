using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class VaccineController : ControllerBase
    {
        private readonly IVaccineService _vaccineService;
        private readonly ILoggerService _logger;
        public VaccineController(IVaccineService vaccineService, ILoggerService logger)
        {
            _vaccineService = vaccineService;
            _logger = logger;
        }

        [HttpPost("create")]
        [Authorize(Policy = "StaffPolicy")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Create([FromBody] VaccineDTO vaccineDTO)
        {
            _logger.Info("Create vaccine request received.");

            if (vaccineDTO == null)
            {
                _logger.Warn("CreateVaccine: Vaccine data is null.");
                return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
            }
            try
            {
                _logger.Info($"CreateVaccine: Attempting to create a new vaccine - {vaccineDTO.VaccineName}.");

                var createdVaccine = await _vaccineService.CreateVaccine(vaccineDTO);
                
                if(createdVaccine == null)
                {
                    _logger.Warn("CreateVaccine: Vaccine creation failed due to validation issues.");
                    return BadRequest(ApiResult<object>.Error("400 - Vaccine creation failed. Please check input data."));
                }

                _logger.Success($"CreateVaccine: Vaccine '{createdVaccine.VaccineName}' created successfully.");

                return Ok(ApiResult<object>.Success(new
                {
                   vaccineName = createdVaccine.VaccineName,
                   vaccineDes = createdVaccine.Description,
                   vaccinePicUrl = createdVaccine.PicUrl,
                   vaccineType = createdVaccine.Type,
                   vaccinePrice = createdVaccine.Price
                }, "Creation successful."));
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error during creation: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creation."));
            }
        }
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "StaffPolicy")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Update(Guid id, [FromBody] VaccineDTO vaccineDTO)
        {
            _logger.Info($"Updated vaccine with ID {id} request received");

            if (vaccineDTO == null)
            {
                _logger.Warn("VaccineDTO is null.");
                return BadRequest(ApiResult<object>.Error("400 - Vaccine data cannot be null."));
            }

            try
            {
                var updateVaccine = await _vaccineService.UpdateVaccine(id, vaccineDTO);
                return Ok(ApiResult<VaccineDTO>.Success(updateVaccine, "Vaccine updated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.Warn(ex.Message);
                return NotFound(ApiResult<object>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error during update: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during update."));
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "StaffPolicy")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.Info($"Delete vaccine with ID {id} request received.");
            try
            {
                var deletedVaccine = await _vaccineService.DeleteVaccine(id);

                if (deletedVaccine == null)
                {
                    _logger.Warn($"DeleteVaccine: Failed to delete vaccine with ID {id} due to validation issues.");
                    return BadRequest(ApiResult<object>.Error("400 - Vaccine deleting failed. Please check input data."));
                }

                _logger.Success($"DeleteVaccine: Vaccine with name '{deletedVaccine.VaccineName}' deleted successfully.");

                return Ok(ApiResult<Vaccine>.Success(deletedVaccine, "Vaccine deleted successfully."));
            }
            catch (ValidationException ex)
            {
                _logger.Warn($"DeleteVaccine: Validation error while deleting vaccine with ID {id}. Error: {ex.Message}");
                return BadRequest(ApiResult<object>.Error($"400 - Validation error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error during deletion: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during deletion."));
            }

        }
    }