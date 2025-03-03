using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IAuthService
{
    Task<User?> RegisterAsync(RegisterRequestDTO registerRequest);
    Task<LoginResponseDTO?> LoginAsync(LoginRequestDto loginDTO, IConfiguration configuration);
    Task<bool> LogoutAsync(Guid userId);
}