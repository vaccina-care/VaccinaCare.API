namespace VaccinaCare.Domain.DTOs.VaccineDTOs;

public class CheckVaccinesDto
{
    public List<Guid> VaccineIds { get; set; } = new();
}