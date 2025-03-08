﻿using VaccinaCare.Application.Interface;
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

    public async Task<NotificationForAppointmentDTO> PushNotificationAppointmentRemider(Guid userId, Guid appointmentId)
    {
        try
        {
            var notification = new Notification
            {
                Title = "Upcoming Appointment Reminder",
                Content = "Your appointment is scheduled soon. Please be prepared.",
                IsRead = false,
                UserId = userId,
                AppointmentId = appointmentId,
                Type = NotificationType.ByUser,
                Role = "User"
            };

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Reminder notification successfully sent to user {userId}: {notification.Title}");

            return new NotificationForAppointmentDTO
            {
                Title = notification.Title,
                Content = notification.Content,
                IsRead = notification.IsRead,
                AppointmentId = notification.AppointmentId.GetValueOrDefault()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error pushing reminder notification to user {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<NotificationForAppointmentDTO> PushNotificationAppointmentSuccess(Guid userId, Guid appointmentId)
    {
        try
        {
            var notification = new Notification
            {
                Title = "Appointment Booking Successful",
                Content = "Your appointment has been successfully booked.",
                IsRead = false,
                UserId = userId,
                AppointmentId = appointmentId,
                Type = NotificationType.ByUser,
                Role = "User"
            };

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Notification successfully sent to user {userId}: {notification.Title}");

            return new NotificationForAppointmentDTO
            {
                Title = notification.Title,
                Content = notification.Content,
                IsRead = notification.IsRead,
                AppointmentId = notification.AppointmentId.GetValueOrDefault()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error pushing notification to user {userId}: {ex.Message}");
            throw;
        }
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

    public async Task<Notification> PushNotificationWhenUserUseService(Guid userId,
        NotificationForUserDTO notificationDTO)
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
                Type = NotificationType.ByUser,
                Role = "User"
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

    public async Task<Notification> PushPaymentSuccessNotification(Guid userId, NotificationForUserDTO notificationDTO)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                _logger.Warn($"Invalid payment notification data for user {userId}");
                throw new ArgumentException("User ID are required.");
            }

            var notification = new Notification
            {
                Title = "Payment Successful -  Thank You",
                Content = "Your payment has been successfully processed. Thank you!",
                IsRead = false,
                UserId = userId,
                Type = NotificationType.ByUser
            };

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Payment success notification sent to user {userId} : {notification.Title}");
            return notification;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error pushing payment success notification to user {userId}: {ex.Message}");
            throw;
        }
    }
}