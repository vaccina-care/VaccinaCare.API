using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int? UserId { get; set; }

    public int? PaymentId { get; set; }

    public decimal? TotalAmount { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual User? User { get; set; }
}
