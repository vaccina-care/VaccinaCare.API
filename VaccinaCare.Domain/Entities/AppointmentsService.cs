using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class AppointmentsService
{
    public int AppointmentServiceId { get; set; }

    public int? AppointmentId { get; set; }

    public int? ServiceId { get; set; }

    public int? Quantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Service? Service { get; set; }
}
