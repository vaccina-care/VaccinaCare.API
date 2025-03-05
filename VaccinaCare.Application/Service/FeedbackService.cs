using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.FeedbackDTOs;
using VaccinaCare.Repository.Interfaces;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;
using Microsoft.EntityFrameworkCore;

namespace VaccinaCare.Application.Service;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;
    private readonly IClaimsService _claimsService;

    public FeedbackService(IUnitOfWork unitOfWork, ILoggerService logger, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimsService = claimsService;
    }

    // public async Task<List<FeedbackDTO>> GetFeedbacksByUserId(Guid userId)
    // {
    //     try
    //     {
    //         var feedbacks = _unitOfWork.FeedbackRepository.GetQueryable().Where(u => u.UserId == userId).ToList();
    //
    //         foreach (var feedback in feedbacks)
    //         {
    //             return new List<FeedbackDTO>()
    //             {
    //                  = feedbacks.AppointmentId.GetValueOrDefault(),
    //                 Rating = feedback.Rating.GetValueOrDefault(),
    //                 Comments = feedback.Comments
    //             };
    //         }
    //         
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //         throw;
    //     }
    // }

    public async Task<FeedbackDTO> CreateFeedbackAsync(FeedbackDTO feedbackDto)
    {
        try
        {
            var userId = _claimsService.GetCurrentUserId;
            _logger.Info($"User {userId} is attepting to create feedback.");

            var appointment = await _unitOfWork.AppointmentRepository.GetByIdAsync(feedbackDto.AppointmentId);

            if (appointment == null)
            {
                _logger.Warn($"Appointment {feedbackDto.AppointmentId} not found");
                throw new KeyNotFoundException("Appointment not found.");
            }

            if (appointment.ParentId != userId)
            {
                _logger.Warn(
                    $"User {userId} is not authorized to leave feedback for Appointment {feedbackDto.AppointmentId}.");
                throw new UnauthorizedAccessException("You are not authorized to give feedback for this appointment.");
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                _logger.Warn($"Appointment {feedbackDto.AppointmentId} is not completed. Feedback not allowed.");
                throw new InvalidOperationException("Feedback can only be given for completed appointments.");
            }

            if (feedbackDto.Rating < 1 || feedbackDto.Rating > 5)
            {
                _logger.Warn($"Invalid rating: {feedbackDto.Rating}. Rating must be between 1 and 5.");
                throw new ArgumentOutOfRangeException(nameof(feedbackDto.Rating), "Rating must be between 1 and 5.");
            }


            var feedback = new Feedback
            {
                AppointmentId = feedbackDto.AppointmentId,
                Rating = feedbackDto.Rating,
                Comments = feedbackDto.Comments ?? "No comments provided.",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FeedbackRepository.AddAsync(feedback);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"User {userId} added feedback for Appointment {feedbackDto.AppointmentId}");

            return new FeedbackDTO
            {
                AppointmentId = feedback.AppointmentId.GetValueOrDefault(),
                Rating = feedback.Rating.GetValueOrDefault(),
                Comments = feedback.Comments
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in CreateFeedbackAsync: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteFeedbackAsync(Guid feedbackId)
    {
        {
            try
            {
                _logger.Info($"Attempting to delete feedback {feedbackId}.");

                var feedback = await _unitOfWork.FeedbackRepository.GetByIdAsync(feedbackId);

                if (feedback == null)
                {
                    _logger.Warn($"Feedback {feedbackId} not found.");
                    throw new KeyNotFoundException("Feedback not found.");
                }

                await _unitOfWork.FeedbackRepository.SoftRemove(feedback);
                await _unitOfWork.SaveChangesAsync();

                _logger.Info($"Successfully deleted feedback {feedbackId}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred while deleting feedback {feedbackId}: {ex.Message}");
                throw;
            }
        }
    }

    public async Task<Pagination<FeedbackDTO>> GetAllFeedbacksAsync(PaginationParameter pagination)
    {
        try
        {
            _logger.Info(
                $"Fetching feedback with pagination: Page {pagination.PageIndex}, Size {pagination.PageSize} ");

            var query = _unitOfWork.FeedbackRepository.GetQueryable();

            var totalFeedbacks = await query.CountAsync();

            var feedbacks = await query
                .OrderBy(f => f.Rating)
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            if (!feedbacks.Any())
            {
                _logger.Warn($"No feedbacks found on page {pagination.PageIndex}.");
                return new Pagination<FeedbackDTO>(new List<FeedbackDTO>(), 0, pagination.PageIndex,
                    pagination.PageSize);
            }

            _logger.Success($"Retrieved {feedbacks.Count} feedbacks on page {pagination.PageIndex}");

            var feedfackDtos = feedbacks.Select(feedback => new FeedbackDTO
            {
                AppointmentId = feedback.AppointmentId.GetValueOrDefault(),
                Rating = feedback.Rating.GetValueOrDefault(),
                Comments = feedback.Comments
            }).ToList();

            return new Pagination<FeedbackDTO>(feedfackDtos, totalFeedbacks, pagination.PageIndex, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching feedbacks: {ex.Message}");
            throw new Exception("An error occurred while fetching feedbacks. Please try again later");
        }
    }

    public async Task<FeedbackDTO> GetFeedbackByIdAsync(Guid feedbackId)
    {
        try
        {
            _logger.Info($"Fetching feedback with ID: {feedbackId}");

            var feedback = await _unitOfWork.FeedbackRepository.GetByIdAsync(feedbackId);

            if (feedback == null)
            {
                _logger.Warn($"Feedback {feedbackId} not found.");
                throw new KeyNotFoundException("Feedback not found.");
            }

            return new FeedbackDTO
            {
                AppointmentId = feedback.AppointmentId.GetValueOrDefault(),
                Rating = feedback.Rating.GetValueOrDefault(),
                Comments = feedback.Comments
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while fetching feedback {feedbackId}");
            throw;
        }
    }

    public async Task<FeedbackDTO> UpdateFeedbackAsync(Guid feedbackId, FeedbackDTO feedbackDto)
    {
        try
        {
            var userId = _claimsService.GetCurrentUserId;
            _logger.Info($"User {userId} is attempting to update feedback {feedbackId}.");

            var feedback = await _unitOfWork.FeedbackRepository.GetByIdAsync(feedbackId);

            if (feedback == null)
            {
                _logger.Warn($"Feedback {feedbackId} not found.");
                throw new KeyNotFoundException("Feedback not found.");
            }

            var appointment =
                await _unitOfWork.AppointmentRepository.GetByIdAsync(feedback.AppointmentId.GetValueOrDefault());
            // Chỗ này nên xem lại admin có quyền xóa lịch sử lịch trình đã hoàn thành của khách hàng hay không
            if (appointment == null)
            {
                _logger.Warn($"Appointment {feedback.AppointmentId} not found.");
                throw new KeyNotFoundException("Appointment not found.");
            }

            if (appointment.ParentId.GetValueOrDefault() != userId)
            {
                _logger.Warn(
                    $"User {userId} is not authorized to update feedback for Appointment {feedback.AppointmentId}.");
                throw new UnauthorizedAccessException("You are not authorized to update this feedback.");
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                _logger.Warn($"Appointment {feedback.AppointmentId} is not completed. Feedback cannot be updated.");
                throw new InvalidOperationException("Feedback can only be updated for completed appointments.");
            }

            if ((DateTime.UtcNow - feedback.CreatedAt).TotalHours > 24)
            {
                _logger.Warn($"Feedback {feedbackId} cannot be updated after 24 hours.");
                throw new InvalidOperationException("Feedback can only be updated within 24 hours after submission.");
            }

            if (feedbackDto.Rating < 1 || feedbackDto.Rating > 5)
            {
                _logger.Warn($"Invalid rating: {feedbackDto.Rating}. Rating must be between 1 and 5.");
                throw new ArgumentException("Rating must be between 1 and 5.");
            }

            feedback.Rating = feedbackDto.Rating;
            feedback.Comments = feedbackDto.Comments ?? feedback.Comments;
            feedback.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.FeedbackRepository.Update(feedback);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"User {userId} successfully updated feedback {feedbackId}.");

            return new FeedbackDTO
            {
                AppointmentId = feedback.AppointmentId.GetValueOrDefault(),
                Rating = feedback.Rating.GetValueOrDefault(),
                Comments = feedback.Comments
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while updating feedback {feedbackId}: {ex.Message}");
            throw;
        }
    }
}