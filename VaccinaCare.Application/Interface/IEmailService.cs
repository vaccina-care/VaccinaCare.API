using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IEmailService
{
    Task SendWelcomeNewUserAsync(EmailRequestDTO emailRequest);

    Task SendDeactivationNotificationAsync(EmailRequestDTO emailRequest);

    Task SendSingleAppointmentConfirmationAsync(EmailRequestDTO emailRequest, Appointment appointment, Guid vaccineId);

    Task SendPackageAppointmentConfirmationAsync(EmailRequestDTO emailRequest, List<Appointment> appointments,
        Guid packageId);
    Task SendRescheduledAppointmentNotificationAsync(EmailRequestDTO emailRequest, List<AppointmentDTO> appointments);
}