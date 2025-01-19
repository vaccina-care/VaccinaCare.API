using VaccinaCare.Domain.DTOs.AuthDTOs;

namespace VaccinaCare.Application.Interface;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequestDTO registerRequest);
}