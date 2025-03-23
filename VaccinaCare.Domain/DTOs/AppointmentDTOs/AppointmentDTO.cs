namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class AppointmentDTO
{
    public Guid AppointmentId { get; set; }
    public Guid ChildId { get; set; }
    public string ChildName { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; }
    public string VaccineName { get; set; }
    public int DoseNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public string Notes { get; set; }
}