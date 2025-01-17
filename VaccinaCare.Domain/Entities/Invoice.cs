namespace VaccinaCare.Domain.Entities;

public partial class Invoice : BaseEntity
{

    public int? UserId { get; set; }

    public int? PaymentId { get; set; }

    public decimal? TotalAmount { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual User? User { get; set; }
}
