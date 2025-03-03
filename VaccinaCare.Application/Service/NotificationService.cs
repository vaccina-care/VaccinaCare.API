using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class NotificationService : INotificationService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Notification> PushNotificationToUser(Guid userId, NotificationDTO notificationDTO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notificationDTO.Title) || string.IsNullOrWhiteSpace(notificationDTO.Content))
            {
                _logger.Warn($"Invalid notification data for user {userId}. Title and content are required.");
                throw new ArgumentException("Notification title and content are required.");
            }

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

            _logger.Info($"Notification successfully sent to user {userId}: {notification.Title}");
            return notification;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error pushing notification to user {userId}: {ex.Message}");
            throw;
        }
    }
}