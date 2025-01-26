using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface INotificationService
{
    // Push notification to a specific user
    Task<Notification> PushNotificationToUser(Guid userId, NotificationDTO notificationDTO);
}