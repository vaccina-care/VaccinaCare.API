using VaccinaCare.Domain.DTOs.EmailDTOs;

namespace VaccinaCare.Application.Interface;

public interface IEmailService
{
    Task SendWelcomeNewUserAsync(EmailRequestDTO emailRequest);
}