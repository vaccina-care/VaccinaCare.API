using Microsoft.Extensions.Configuration;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterRequestDTO registerRequest);
    Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO loginDTO, IConfiguration configuration);

}