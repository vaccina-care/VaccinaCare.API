namespace VaccinaCare.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid? UserId { get; set; }

    public Guid? PaymentId { get; set; }

    public decimal? TotalAmount { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual User? User { get; set; }
}