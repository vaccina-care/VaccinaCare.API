namespace VaccinaCare.Domain.DTOs.VaccineDTOs;

public class CheckCompatibilityRequest
{
    public Guid VaccineId { get; set; }
    public List<Guid?> BookedVaccineIds { get; set; }
    public DateTime AppointmentDate { get; set; }
}