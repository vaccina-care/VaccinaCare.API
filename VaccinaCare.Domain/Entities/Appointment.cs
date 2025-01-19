namespace VaccinaCare.Domain.Entities;

public partial class Appointment : BaseEntity
{
    public int? ParentId { get; set; }
    public int? ChildId { get; set; }
    public int? PolicyId { get; set; } // Khóa ngoại liên kết với CancellationPolicy
    public DateTime? AppointmentDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? VaccineType { get; set; }
    public int? Duration { get; set; }
    public string? Room { get; set; }
    public bool? ReminderSent { get; set; }
    public string? CancellationReason { get; set; }
    public bool? Confirmed { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? PreferredTimeSlot { get; set; }

    public virtual CancellationPolicy? CancellationPolicies { get; set; } // Tham chiếu đến CancellationPolicy
    public virtual Child? Child { get; set; } // Tham chiếu đến CancellationPolicy
    public virtual ICollection<AppointmentsVaccine> AppointmentsVaccines { get; set; } = new List<AppointmentsVaccine>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

}

