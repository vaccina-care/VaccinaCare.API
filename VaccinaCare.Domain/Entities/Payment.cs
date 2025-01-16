using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? AppointmentId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentStatus { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
