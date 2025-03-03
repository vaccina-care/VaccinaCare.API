namespace VaccinaCare.Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string? MethodName { get; set; } // e.g., VnPay, PayOs, Bank Transfer
    public string? Description { get; set; }
    public bool? Active { get; set; }

    // Navigation property to Payments
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}