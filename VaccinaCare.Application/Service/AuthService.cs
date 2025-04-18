﻿using Microsoft.Extensions.Configuration;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class AuthService : IAuthService
{
    private readonly IEmailService _emailService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork, ILoggerService logger, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
    {
        _logger.Info("Login process initiated.");

        try
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                _logger.Warn("Login attempt failed: Missing email or password. Both fields are required.");
                return null;
            }

            _logger.Info($"Login request received for email: {loginDto.Email}");

            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(
                u => u.Email == loginDto.Email && !u.IsDeleted
            );

            if (user == null)
            {
                _logger.Warn($"Login attempt failed: No active user found with email: {loginDto.Email}.");
                return null;
            }

            var passwordHasher = new PasswordHasher();
            if (!passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                _logger.Warn($"Login attempt failed: Invalid password for email: {loginDto.Email}.");
                return null;
            }

            _logger.Info($"User found for email: {loginDto.Email}. Verifying user role and generating tokens.");

            var roleName = user.RoleName.ToString();
            _logger.Info($"Role '{roleName}' identified for user with email: {loginDto.Email}.");

            var accessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                roleName,
                configuration,
                TimeSpan.FromMinutes(30)
            );

            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _logger.Info(
                $"Tokens successfully generated for user with email: {loginDto.Email}. Updating user record with refresh token.");

            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info(
                $"User record successfully updated with refresh token for email: {loginDto.Email}. Login process completed successfully.");

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Unexpected error during login process for email: {loginDto?.Email ?? "Unknown"}. Exception: {ex.Message}");
            _logger.Error($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<User?> RegisterAsync(RegisterRequestDTO registerRequest)
    {
        try
        {
            _logger.Info("Starting registration process.");

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                _logger.Warn("Email or Password is missing in the registration request.");
                return null;
            }

            _logger.Info($"Validated required fields for email: {registerRequest.Email}");

            // Kiểm tra email đã tồn tại chưa
            var existingUser =
                await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                _logger.Warn($"Registration attempt failed. Email {registerRequest.Email} is already in use.");
                return null;
            }

            _logger.Info($"Email {registerRequest.Email} is available for registration.");

            // Hash mật khẩu
            _logger.Info("Hashing the password.");
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword(registerRequest.Password);

            // Tạo user mới
            _logger.Info("Creating new user object.");
            var newUser = new User
            {
                Email = registerRequest.Email.Trim(),
                FullName = registerRequest.FullName?.Trim(),
                PhoneNumber = registerRequest.PhoneNumber?.Trim(),
                Gender = registerRequest.Gender,
                PasswordHash = hashedPassword,
                Address = registerRequest.Address?.Trim(),
                DateOfBirth = registerRequest.DateOfBirth,
                ImageUrl = registerRequest.ImageUrl,
                RoleName = RoleType.Customer // Lưu trực tiếp RoleType (enum)
            };

            // Lưu user vào database
            _logger.Info("Saving the new user to the database.");
            await _unitOfWork.UserRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"User {registerRequest.Email} successfully registered.");

            // Gọi hàm gửi email chào mừng người dùng mới
            var emailRequest = new EmailRequestDTO
            {
                UserEmail = newUser.Email,
                UserName = newUser.FullName
            };
            await _emailService.SendWelcomeNewUserAsync(emailRequest);

            return newUser;
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"An unexpected error occurred during registration for email {registerRequest?.Email ?? "Unknown"}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        _logger.Info($"Logout process initiated for user ID: {userId}");

        try
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                _logger.Warn($"Logout attempt failed: No active user found with ID: {userId}.");
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            _logger.Info($"Clearing refresh token for user ID: {userId}.");

            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Logout successful for user ID: {userId}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during logout for user ID: {userId}. Exception: {ex.Message}");
            _logger.Error($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<LoginResponseDTO?> RefreshTokenAsync(TokenRefreshRequestDTO tokenRequest,
        IConfiguration configuration)
    {
        _logger.Info("Refresh token process initiated.");

        try
        {
            if (string.IsNullOrWhiteSpace(tokenRequest.RefreshToken))
            {
                _logger.Warn("Missing refresh token.");
                return null;
            }

            _logger.Info($"Received refresh token: {tokenRequest.RefreshToken}");

            // Tìm user có refresh token trong database
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(u =>
                u.RefreshToken == tokenRequest.RefreshToken);

            if (user == null)
            {
                _logger.Warn("User not found with the provided refresh token.");
                return null;
            }

            // Kiểm tra Refresh Token có hợp lệ không (thời gian hết hạn)
            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                _logger.Warn("Refresh token invalid or expired.");
                return null;
            }

            _logger.Info($"Valid refresh token for user {user.Email}. Generating new tokens...");

            // 🛑 Lấy role của user
            var roleName = user.RoleName.ToString();

            // 🛑 Tạo Access Token mới (1 giờ)
            var newAccessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                roleName,
                configuration,
                TimeSpan.FromHours(1)
            );

            // 🛑 Tạo Refresh Token mới (Valid trong 7 ngày)
            var newRefreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _logger.Info($"Generated new tokens for user {user.Email}. Updating database.");

            // 🛑 Lưu refresh token mới vào database
            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Refresh token updated successfully for user {user.Email}.");

            return new LoginResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during refresh token process: {ex.Message}");
            _logger.Error($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}