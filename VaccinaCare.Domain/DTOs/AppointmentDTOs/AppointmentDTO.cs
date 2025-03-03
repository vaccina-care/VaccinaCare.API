using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class AppointmentDTO
{
    public Guid AppointmentId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; }
    public string VaccineName { get; set; }
    public int DoseNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public string Notes { get; set; }
}