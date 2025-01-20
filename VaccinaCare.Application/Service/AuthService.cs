using Microsoft.Extensions.Configuration;
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
            var existingUser =
                await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
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

    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO loginDTO, IConfiguration configuration)
    {
        _logger.Info("Login attempt initiated.");
        try
        {
            if (string.IsNullOrWhiteSpace(loginDTO.Email) || string.IsNullOrWhiteSpace(loginDTO.Password))
            {
                _logger.Warn("Login failed due to missing email or password.");
                return null;
            }
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(
                u => u.Email == loginDTO.Email && !u.IsDeleted
            );

            if (user == null)
            {
                _logger.Warn($"Login failed. User with email {loginDTO.Email} not found.");
                return null;
            }
            var passwordHasher = new PasswordHasher();
            if (!passwordHasher.VerifyPassword(loginDTO.Password, user.PasswordHash))
            {
                _logger.Warn($"Login failed. Invalid password for user with email {loginDTO.Email}.");
                return null;
            }
            var accessToken = JwtUtils.GenerateJwtToken(
                user.Id.ToString(),
                user.Email,
                user.Role?.RoleName?? "Unknown",
                configuration,
                TimeSpan.FromMinutes(30)
            );
            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            _logger.Info($"User {loginDTO.Email} successfully logged in.");
            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred during login: {ex.Message}");
            throw;
        }
    }
}