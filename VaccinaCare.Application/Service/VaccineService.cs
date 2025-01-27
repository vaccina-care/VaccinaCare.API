using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
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
                //Validate input
                if (string.IsNullOrEmpty(vaccineDTO.VaccineName))
                {
                    _logger.Warn("Vaccine name is required.");
                    return null;
                }
                if (vaccineDTO.Price < 0)
                {
                    _logger.Warn("Price cannot be negative.");
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

                await _unitOfWork.VaccineRepository.AddAsync(vaccine);
                await _unitOfWork.SaveChangesAsync();
                _logger.Info($"Vaccine '{vaccine.VaccineName}' created successfully.");

                return vaccine;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred while creating the vaccine.{ex.Message}");
                throw;
            }
        }

        public async Task<Vaccine> DeleteVaccine(Guid id)
        {
            _logger.Info($"Delete vaccine with ID: {id}");
            try
            {
                var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);
                if (vaccine == null)
                {
                    _logger.Warn($"Vaccine with ID: {id} not found.");
                    throw new KeyNotFoundException($"Vaccine with ID {id} not found.");
                }

                await _unitOfWork.VaccineRepository.SoftRemove(vaccine);
                await _unitOfWork.SaveChangesAsync();

                _logger.Success($"Vaccine with ID {id} deleted successfully.");
                return vaccine;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred while deleting the vaccine with ID {id}. Error: {ex.Message}");
                throw;
            }
        }

        public async Task<Vaccine> UpdateVaccine(Guid id, VaccineDTO vaccineDTO)
        {
            _logger.Info($"Updated vaccine with ID: {id}");

            if (vaccineDTO == null)
            throw new NullReferenceException("400 - Vaccine data cannot be null.");
            
            try
            {
                var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);
                if (vaccine == null)
                {
                    _logger.Warn($"Vaccine with ID: {id} not found.");
                    throw new KeyNotFoundException($"Vaccine with ID {id} not found.");
                }

                vaccine.VaccineName = vaccineDTO.VaccineName;
                vaccine.Description = vaccineDTO.Description;
                vaccine.PicUrl = vaccineDTO.PicUrl;
                vaccine.Type = vaccineDTO.Type;
                vaccine.Price = vaccineDTO.Price;

                await _unitOfWork.VaccineRepository.Update(vaccine);
                await _unitOfWork.SaveChangesAsync();

                _logger.Success("Vaccine updated successfully.");
                return vaccine;
            }catch(Exception ex)
            {
                _logger.Error($"500 -  Error during vaccine update: {ex.Message}");
                throw;
            }
        }
    }
}
