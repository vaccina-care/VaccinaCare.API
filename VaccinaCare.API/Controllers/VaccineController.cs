using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
namespace VaccinaCare.API.Controllers
{
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
        public async Task<IActionResult> Create ([FromBody] VaccineDTO vaccineDTO)
        {
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
    }
}