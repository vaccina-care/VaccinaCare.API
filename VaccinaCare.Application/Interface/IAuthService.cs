using Microsoft.Extensions.Configuration;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IAuthService
{
    Task<User?> RegisterAsync(RegisterRequestDTO registerRequest);

    Task<LoginResponseDTO?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);

    Task<bool> LogoutAsync(Guid userId);

    Task<LoginResponseDTO?> RefreshTokenAsync(TokenRefreshRequestDTO tokenRequest,
        IConfiguration configuration);
}