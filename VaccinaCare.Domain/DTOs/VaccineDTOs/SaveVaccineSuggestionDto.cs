namespace VaccinaCare.Domain.DTOs.VaccineDTOs;

public class SaveVaccineSuggestionDto
{
    public Guid ChildId { get; set; }
    public List<Guid> VaccineIds { get; set; } = new List<Guid>();
}
