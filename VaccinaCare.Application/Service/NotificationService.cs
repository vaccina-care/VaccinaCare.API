using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Implement;

public class NotificationService : INotificationService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Notification> PushNotificationToUser(int userId, NotificationDTO notificationDTO)
    {
        try
        {
            var notification = new Notification
            {
                Title = notificationDTO.Title,
                Content = notificationDTO.Content,
                Url = notificationDTO.Url,
                IsRead = false,
                UserId = userId,
                Type = NotificationType.ByUser
            };

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Notification sent to user {userId}: {notification.Title}");
            return notification;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error pushing notification to user {userId}: {ex.Message}");
            throw;
        }
    }


}