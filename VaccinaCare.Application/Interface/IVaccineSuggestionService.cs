namespace VaccinaCare.Application.Interface;

public interface IVaccineSuggestionService
{
    Task GenerateVaccineSuggestionsAsync(Guid childId);
}