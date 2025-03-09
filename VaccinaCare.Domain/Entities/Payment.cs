using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public partial class Payment : BaseEntity
{
    public Guid AppointmentId { get; set; } // Liên kết với Appointment

    public string OrderDescription { get; set; }
    public string OrderId { get; set; }
    public string PaymentMethod { get; set; }
    public string? VnpayPaymentId { get; set; }
    public string? TransactionId { get; set; }

    public virtual Appointment Appointment { get; set; } // Navigation property
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}