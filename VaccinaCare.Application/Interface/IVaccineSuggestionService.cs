namespace VaccinaCare.Application.Interface;

public interface IVaccineSuggestionService
{
    Task<bool> SaveVaccineSuggestionAsync(Guid childId, List<Guid> vaccineIds);
}