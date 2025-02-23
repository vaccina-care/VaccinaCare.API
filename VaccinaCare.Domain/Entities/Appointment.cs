using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid? ParentId { get; set; }
    public Guid? ChildId { get; set; }
    public Guid? PolicyId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public VaccineType VaccineType { get; set; } = VaccineType.SingleDose;
    public Guid? VaccineSuggestionId { get; set; }
    public virtual VaccineSuggestion? VaccineSuggestion { get; set; }
    public string? Notes { get; set; }
    public bool NotificationSent { get; set; } = false;
    public string? CancellationReason { get; set; }
    public virtual CancellationPolicy? CancellationPolicies { get; set; }
    public virtual Child? Child { get; set; }
    public virtual ICollection<AppointmentsVaccine> AppointmentsVaccines { get; set; } = new List<AppointmentsVaccine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    
    public virtual ICollection<AppointmentVaccineSuggestions> AppointmentVaccineSuggestions { get; set; } = new List<AppointmentVaccineSuggestions>();
}


