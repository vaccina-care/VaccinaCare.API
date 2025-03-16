using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.FeedbackDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface;

public interface IFeedbackService
{
    Task<FeedbackDTO> CreateFeedbackAsync(FeedbackDTO feedbackDto);
    Task<FeedbackDTO> UpdateFeedbackAsync(Guid feedbackId, FeedbackDTO feedbackDto);
    Task DeleteFeedbackAsync(Guid feedbackId);
    Task<List<GetFeedbackDto>> GetFeedbackByUserIdAsync();
    Task<Pagination<GetFeedbackDto>> GetAllFeedbacksAsync(PaginationParameter pagination);
}