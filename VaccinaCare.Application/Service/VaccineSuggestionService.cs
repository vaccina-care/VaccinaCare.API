using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineSuggestionService : IVaccineSuggestionService
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public VaccineSuggestionService(ILoggerService logger, IUnitOfWork unitOfWork,
        IAppointmentService appointmentService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _appointmentService = appointmentService;
    }

    /// <summary>
    ///     Sau khi tư vấn, staff sẽ cho Parent 1 list các vaccine để chích và User sẽ xài List này để book Appointment
    /// </summary>
    /// <param name="childId"></param>
    /// <param name="vaccineIds"></param>
    /// <returns></returns>
    public async Task<List<VaccineSuggestion>> SaveVaccineSuggestionAsync(Guid childId, List<Guid> vaccineIds)
    {
        try
        {
            _logger.Info($"Saving vaccine suggestions for child ID: {childId}");

            // Check if child exists
            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (child == null)
            {
                _logger.Warn($"Child with ID {childId} not found.");
                throw new Exception("Child not found.");
            }

            // Retrieve the latest pending appointment for the child
            var appointment = await _unitOfWork.AppointmentRepository
                .FirstOrDefaultAsync(a => a.ChildId == childId && a.Status == AppointmentStatus.Pending);

            if (appointment == null)
            {
                _logger.Warn($"No pending appointment found for child ID: {childId}");
                throw new Exception("No pending appointment found.");
            }

            // Create vaccine suggestions if they don't already exist
            var vaccineSuggestions = new List<VaccineSuggestion>();

            foreach (var vaccineId in vaccineIds)
            {
                var existingSuggestion = await _unitOfWork.VaccineSuggestionRepository
                    .FirstOrDefaultAsync(vs => vs.ChildId == childId && vs.VaccineId == vaccineId);

                if (existingSuggestion == null) // Avoid duplicate suggestions
                {
                    var newSuggestion = new VaccineSuggestion
                    {
                        Id = Guid.NewGuid(),
                        ChildId = childId,
                        VaccineId = vaccineId,
                        Status = "Suggested",
                        CreatedAt = DateTime.UtcNow
                    };
                    vaccineSuggestions.Add(newSuggestion);
                }
            }

            if (vaccineSuggestions.Any())
            {
                await _unitOfWork.VaccineSuggestionRepository.AddRangeAsync(vaccineSuggestions);
                await _unitOfWork.SaveChangesAsync();
            }

            // Link suggestions to the appointment
            var appointmentVaccineSuggestions = vaccineSuggestions
                .Select(vs => new AppointmentVaccineSuggestions
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    VaccineSuggestionId = vs.Id
                }).ToList();

            if (appointmentVaccineSuggestions.Any())
            {
                await _unitOfWork.AppointmentVaccineSuggestionsRepository.AddRangeAsync(appointmentVaccineSuggestions);
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.Success($"Vaccine suggestions saved successfully for child ID: {childId}");
            return vaccineSuggestions;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error saving vaccine suggestions for child ID {childId}: {ex.Message}");
            throw;
        }
    }
}