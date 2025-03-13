using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IEmailService
{
    Task SendWelcomeNewUserAsync(EmailRequestDTO emailRequest);
    Task SendAppointmentConfirmationAsync(EmailRequestDTO emailRequest, Appointment appointment);
    Task SendDeactivationNotificationAsync(EmailRequestDTO emailRequest);
    
}