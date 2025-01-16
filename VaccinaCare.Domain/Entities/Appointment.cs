using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int? ParentId { get; set; }

    public int? ChildId { get; set; }

    public int? PolicyId { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public string? ServiceType { get; set; }

    public int? Duration { get; set; }

    public string? Room { get; set; }

    public bool? ReminderSent { get; set; }

    public string? CancellationReason { get; set; }

    public bool? Confirmed { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? PreferredTimeSlot { get; set; }

    public virtual ICollection<AppointmentsService> AppointmentsServices { get; set; } = new List<AppointmentsService>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
