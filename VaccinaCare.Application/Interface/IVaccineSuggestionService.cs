using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IVaccineSuggestionService
{
    Task<List<VaccineSuggestion>> SaveVaccineSuggestionAsync(Guid childId, List<Guid> vaccineIds);
}