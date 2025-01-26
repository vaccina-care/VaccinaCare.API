namespace VaccinaCare.Domain.Entities;

public partial class Payment : BaseEntity
{

    public Guid? AppointmentId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentStatus { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
