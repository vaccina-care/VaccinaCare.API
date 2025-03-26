using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface INotificationService
{
    // Push notification to a specific user
    Task<Notification> PushNotificationToUser(Guid userId, NotificationDTO notificationDTO);

    Task<Notification> PushNotificationWhenUserUseService(Guid userId, NotificationForUserDTO notificationDTO);

    Task<NotificationForAppointmentDTO> PushPaymentSuccessNotification(Guid userId, Guid appointmentId);

    Task<NotificationForAppointmentDTO> PushNotificationAppointmentSuccess(Guid userId, Guid appointmentId);

    Task<NotificationForAppointmentDTO> PushNotificationAppointmentRemider(Guid userId, Guid appointmentId);
}