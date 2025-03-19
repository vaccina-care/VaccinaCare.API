namespace VaccinaCare.Domain.DTOs.VaccineRecordDTOs;

public class UpdateVaccineRecorđto
{
    public Guid ChildId { get; set; }
    public Guid VaccineId { get; set; }
    public string? ReactionDetails { get; set; }
    public int DoseNumber { get; set; }
}