using System;
using System.Collections.Generic;
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

        public Task<Vaccine> DeleteVaccine(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Vaccine> UpdateVaccine(int id, VaccineDTO vaccineDTO)
        {
            throw new NotImplementedException();
        }
    }
}
