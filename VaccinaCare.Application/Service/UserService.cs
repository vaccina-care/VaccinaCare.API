﻿using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
    public class UserService : IUserService
    {
        private readonly ILoggerService _logger;
        private readonly IUnitOfWork _unitOfWork;
        public UserService(ILoggerService logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                // Log the start of the operation
                _logger.Info("Fetching all users from the database.");

                // Fetch all users from the repository
                var users = await _unitOfWork.UserRepository.GetAllAsync();

                // Log the success of the operation
                _logger.Info($"Successfully fetched {users.Count()} users.");

                return users;
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.Error($"An error occurred while fetching users: {ex.Message}");
                throw;
            }
        }
    }
}
