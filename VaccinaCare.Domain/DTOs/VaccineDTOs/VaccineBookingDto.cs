namespace VaccinaCare.Domain.DTOs.VaccineDTOs;

public class VaccineBookingDto
{
    public Guid VaccineId { get; set; }
    public string VaccineName { get; set; }
    public int BookingCount { get; set; }
}