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
            _logger.Info("Starting registration process.");

            if (string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                _logger.Warn("Email or Password is missing in the registration request.");
                return null;
            }

            _logger.Info($"Validated required fields for email: {registerRequest.Email}");

            var existingUser =
                await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                _logger.Warn($"Registration attempt failed. Email {registerRequest.Email} is already in use.");
                return null;
            }

            _logger.Info($"Email {registerRequest.Email} is available for registration.");

            _logger.Info("Hashing the password.");
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword(registerRequest.Password);

            _logger.Info("Creating new user object.");
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

            _logger.Info("Saving the new user to the database.");
            await _unitOfWork.UserRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User {registerRequest.Email} successfully registered.");
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"An unexpected error occurred during registration for email {registerRequest?.Email ?? "Unknown"}: {ex.Message}");
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
                _logger.Warn("Login failed due to missing email or password. Both fields are required.");
                return null;
            }

            _logger.Info($"Login request received for email: {loginDTO.Email}");

            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(
                u => u.Email == loginDTO.Email && !u.IsDeleted
            );
            if (user == null)
            {
                _logger.Warn(
                    $"Login failed. No active user found for email: {loginDTO.Email}. User may not exist or is marked as deleted.");
                return null;
            }

            _logger.Info($"User found for email: {loginDTO.Email}. Proceeding with password verification.");

            var passwordHasher = new PasswordHasher();
            if (!passwordHasher.VerifyPassword(loginDTO.Password, user.PasswordHash))
            {
                _logger.Warn($"Login failed. Invalid password provided for email: {loginDTO.Email}.");
                return null;
            }

            _logger.Info($"Password verification successful for email: {loginDTO.Email}.");

            _logger.Info($"Generating JWT and refresh tokens for user with email: {loginDTO.Email}.");
            var accessToken = JwtUtils.GenerateJwtToken(
                user.Id.ToString(),
                user.Email,
                user.Role?.RoleName ?? "Unknown",
                configuration,
                TimeSpan.FromMinutes(30)
            );
            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _logger.Info($"Tokens generated successfully for email: {loginDTO.Email}. Updating user record.");

            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            _logger.Info($"User record updated successfully for email: {loginDTO.Email}. Login process completed.");

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"An unexpected error occurred during login for email: {loginDTO?.Email ?? "Unknown"}. Error: {ex.Message}");
            _logger.Error($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}