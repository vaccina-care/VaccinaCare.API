﻿namespace VaccinaCare.Domain.Entities;

public partial class Payment : BaseEntity
{

    public int? AppointmentId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentStatus { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
