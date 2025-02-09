using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class CreateAppointmentDto
{
    public Guid ParentId { get; set; }  
    public Guid ChildId { get; set; }  
    public DateTime AppointmentDate { get; set; } 
    public VaccineType VaccineType { get; set; } // Type of vaccine (SingleDose or Package)
    public List<Guid> AppointmentsVaccines { get; set; } = new List<Guid>(); // List of vaccine IDs
    public decimal TotalPrice { get; set; } // Total cost of the appointment
    public Guid? PolicyId { get; set; } 
    public string? Notes { get; set; } // Additional notes from the parent
    public bool Confirmed { get; set; } = false; 
}