namespace VaccinaCare.Domain.DTOs.VaccineRecordDTOs;

public class VaccineRecordDto
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid VaccineId { get; set; }
    public DateTime? VaccinationDate { get; set; }
    public string? ReactionDetails { get; set; }
    public int DoseNumber { get; set; }
    public string VaccineName { get; set; }
    public string ChildFullName { get; set; }
}