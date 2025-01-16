using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class ServiceAvailability
{
    public int AvailabilityId { get; set; }

    public int? ServiceId { get; set; }

    public DateOnly? Date { get; set; }

    public string? TimeSlot { get; set; }

    public int? Capacity { get; set; }

    public int? Booked { get; set; }

    public virtual Service? Service { get; set; }
}
