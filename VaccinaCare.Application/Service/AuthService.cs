using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    public AuthService(IUnitOfWork unitOfWork, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<User?> RegisterAsync(RegisterRequestDTO registerRequest)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                _logger.Error("Email and Password are required for registration.");
                return null;
            }

            // Check if the email is already registered
            var existingUser = await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                _logger.Error($"Email {registerRequest.Email} is already registered.");
                return null;
            }

            // Hash the password
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword(registerRequest.Password);

            // Create the new user using the DTO values (with defaults applied)
            var newUser = new User
            {
                Email = registerRequest.Email.Trim(),
                PhoneNumber = registerRequest.PhoneNumber?.Trim(),
                Gender = registerRequest.Gender,
                PasswordHash = hashedPassword,
                DateOfBirth = registerRequest.DateOfBirth.Value, 
                ImageUrl = registerRequest.ImageUrl,           
                RoleId = 1, 
            };

            // Save the new user to the database
            await _unitOfWork.UserRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User {registerRequest.Email} successfully registered.");
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during registration: {ex.Message}");
            return null;
        }
    }


    
}