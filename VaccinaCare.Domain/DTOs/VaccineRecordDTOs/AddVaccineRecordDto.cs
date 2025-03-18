namespace VaccinaCare.Domain.DTOs.VaccineRecordDTOs;

public class AddVaccineRecordDto
{
    public Guid ChildId { get; set; }

    public Guid VaccineId { get; set; }
    public string? ReactionDetails { get; set; }

    public DateTime VaccinationDate { get; set; }
    public int DoseNumber { get; set; }
}