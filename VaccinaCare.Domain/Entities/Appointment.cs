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
    public string? Notes { get; set; }
    public int? Duration { get; set; }
    public bool? ReminderSent { get; set; }
    public string? CancellationReason { get; set; }
    public bool? Confirmed { get; set; }
    public decimal? TotalPrice { get; set; }
    public virtual CancellationPolicy? CancellationPolicies { get; set; }
    public virtual Child? Child { get; set; }
    public virtual ICollection<AppointmentsVaccine> AppointmentsVaccines { get; set; } = new List<AppointmentsVaccine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

