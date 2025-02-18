using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineSuggestionService : IVaccineSuggestionService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public VaccineSuggestionService(ILoggerService loggerService, IUnitOfWork unitOfWork)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateVaccineSuggestionsAsync(Guid childId)
    {
        try
        {
            _loggerService.Info($"Starting vaccine suggestion generation for child ID {childId}");

            var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
            if (child == null)
            {
                _loggerService.Warn($"Child with ID {childId} not found.");
                return;
            }

            _loggerService.Info($"Child found: {child.FullName} (ID: {childId}). Fetching vaccine list...");

            var vaccines = await _unitOfWork.VaccineRepository.GetAllAsync();
            _loggerService.Info($"Total vaccines retrieved: {vaccines.Count()}");

            var suggestedVaccines = new List<VaccineSuggestion>();

            foreach (var vaccine in vaccines)
            {
                _loggerService.Info(
                    $"Checking vaccine: {vaccine.VaccineName} (ID: {vaccine.Id}) for child {child.FullName}");

                if (vaccine.ForBloodType.HasValue && vaccine.ForBloodType != child.BloodType)
                {
                    _loggerService.Info($"Skipping {vaccine.VaccineName} - Blood type mismatch.");
                    continue;
                }

                if (vaccine.AvoidChronic == true && child.HasChronicIllnesses)
                {
                    _loggerService.Info($"Skipping {vaccine.VaccineName} - Not suitable for chronic illness.");
                    continue;
                }

                if (vaccine.AvoidAllergy == true && child.HasAllergies)
                {
                    _loggerService.Info($"Skipping {vaccine.VaccineName} - Allergy concerns.");
                    continue;
                }

                if (vaccine.HasDrugInteraction == true && child.HasRecentMedication)
                {
                    _loggerService.Info($"Skipping {vaccine.VaccineName} - Potential drug interaction.");
                    continue;
                }

                if (vaccine.HasSpecialWarning == true && child.HasOtherSpecialCondition)
                {
                    _loggerService.Info($"Skipping {vaccine.VaccineName} - Special condition warning.");
                    continue;
                }

                suggestedVaccines.Add(new VaccineSuggestion
                {
                    Id = Guid.NewGuid(),
                    ChildId = childId,
                    VaccineId = vaccine.Id,
                    Status = "Pending", 
                    CreatedAt = DateTime.UtcNow
                });

                _loggerService.Info($"Vaccine {vaccine.VaccineName} added to suggestions.");
            }

            if (suggestedVaccines.Any())
            {
                _loggerService.Info(
                    $"Saving {suggestedVaccines.Count} vaccine suggestions for child {child.FullName}...");
                await _unitOfWork.VaccineSuggestionRepository.AddRangeAsync(suggestedVaccines);
                await _unitOfWork.SaveChangesAsync();
                _loggerService.Success(
                    $"Successfully generated {suggestedVaccines.Count} vaccine suggestions for child {child.FullName}.");
            }
            else
            {
                _loggerService.Warn($"No suitable vaccines found for child {child.FullName}.");
            }
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while generating vaccine suggestions for child {childId}: {ex.Message}");
            throw new Exception("An error occurred while generating vaccine suggestions. Please try again later.", ex);
        }
    }
}