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
    public async Task<bool> RegisterAsync(RegisterRequestDTO registerRequest)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                _logger.Error("Email and Password are required for registration.");
                return false;
            }

            // Check if email is already registered
            var existingUser = await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                _logger.Error($"Email {registerRequest.Email} is already registered.");
                return false;
            }

            // Create a new PasswordHasher instance
            var passwordHasher = new PasswordHasher();

            // Hash the password
            var hashedPassword = passwordHasher.HashPassword(registerRequest.Password);

            // Create a new User entity
            var newUser = new User
            {
                Email = registerRequest.Email.Trim(),
                FullName = registerRequest.UserName?.Trim(),
                PhoneNumber = registerRequest.PhoneNumber?.Trim(),
                PasswordHash = hashedPassword,
                RoleId = 1, // Default role as Parent (adjust RoleId based on your logic)
            };

            // Add the new user to the database
            await _unitOfWork.UserRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            // Log success
            _logger.Success($"User {registerRequest.Email} successfully registered.");
            return true;
        }
        catch (Exception ex)
        {
            // Log exception
            _logger.Error($"Error during registration: {ex.Message}");
            return false;
        }
    }

    
}