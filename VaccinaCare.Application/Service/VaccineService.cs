using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineService : IVaccineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;

    public VaccineService(IUnitOfWork unitOfWork, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Vaccine> CreateVaccine(VaccineDTO vaccineDTO)
    {
        _logger.Info("Starting to create a new vaccine.");

        try
        {
            _logger.Info($"Received VaccineDTO with VaccineName: {vaccineDTO.VaccineName}, Type: {vaccineDTO.Type}, Price: {vaccineDTO.Price}");

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(vaccineDTO.VaccineName))
                validationErrors.Add("Vaccine name is required.");

            if (vaccineDTO.Price < 0)
                validationErrors.Add("Price cannot be negative.");

            if (string.IsNullOrWhiteSpace(vaccineDTO.Description))
                validationErrors.Add("Description is required.");

            if (string.IsNullOrWhiteSpace(vaccineDTO.Type))
                validationErrors.Add("Type is required.");

            if (string.IsNullOrWhiteSpace(vaccineDTO.PicUrl))
                validationErrors.Add("PicUrl is required.");

            if (validationErrors.Any())
            {
                _logger.Warn($"Validation failed for VaccineDTO: {string.Join("; ", validationErrors)}");
                return null;
            }

            var vaccine = new Vaccine
            {
                VaccineName = vaccineDTO.VaccineName,
                Description = vaccineDTO.Description,
                PicUrl = vaccineDTO.PicUrl,
                Type = vaccineDTO.Type,
                Price = vaccineDTO.Price
            };
            _logger.Info($"Vaccine object created. Ready to save: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            await _unitOfWork.VaccineRepository.AddAsync(vaccine);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine '{vaccine.VaccineName}' created successfully with ID {vaccine.Id}.");
            return vaccine;
        }
        catch (Exception ex)
        {
            // Log the exception with details
            _logger.Error($"An error occurred while creating the vaccine. Error: {ex.Message}");
            throw;
        }
    }


    public async Task<Vaccine> DeleteVaccine(Guid id)
    {
        _logger.Info($"Initiating vaccine deletion process for ID: {id}");

        try
        {
            _logger.Info($"Fetching vaccine details for ID: {id}");
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);
            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID: {id} not found in the database.");
                throw new KeyNotFoundException($"Vaccine with ID {id} not found.");
            }

            _logger.Info($"Vaccine found. Preparing to delete: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            await _unitOfWork.VaccineRepository.SoftRemove(vaccine);

            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine with ID {id} ('{vaccine.VaccineName}') deleted successfully.");

            return vaccine;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Deletion failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while deleting the vaccine with ID {id}. Error: {ex.Message}");
            throw;
        }
    }


    public async Task<Vaccine> UpdateVaccine(Guid id, VaccineDTO vaccineDTO)
    {
        _logger.Info($"Starting the update process for vaccine with ID: {id}");

        if (vaccineDTO == null)
        {
            _logger.Warn("Update failed: VaccineDTO is null.");
            throw new ArgumentNullException(nameof(vaccineDTO), "400 - Vaccine data cannot be null.");
        }

        try
        {
            _logger.Info($"Fetching vaccine details for ID: {id}");
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);

            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID: {id} not found in the database.");
                throw new KeyNotFoundException($"Vaccine with ID {id} not found.");
            }

            _logger.Info($"Vaccine found. Current details: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            vaccine.VaccineName = vaccineDTO.VaccineName;
            vaccine.Description = vaccineDTO.Description;
            vaccine.PicUrl = vaccineDTO.PicUrl;
            vaccine.Type = vaccineDTO.Type;
            vaccine.Price = vaccineDTO.Price;

            _logger.Info($"Updating vaccine to: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            await _unitOfWork.VaccineRepository.Update(vaccine);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine with ID {id} updated successfully.");

            return vaccine;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Update failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"500 - Error during vaccine update for ID {id}: {ex.Message}");
            throw;
        }
    }
}