namespace VaccinaCare.Domain.DTOs.VaccineDTOs;

public class CheckVaccinesDto
{
    public Guid ChildId { get; set; }

    public Guid VaccineId { get; set; }
}