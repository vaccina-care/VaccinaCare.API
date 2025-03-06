using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public partial class Payment : BaseEntity
{
    public Guid? AppointmentId { get; set; }
    public decimal? Amount { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }

    public PaymentType PaymentType { get; set; } // thanh toán cọc hoặc thanh toán đầy đủ
    public DateTime? PaymentDate { get; set; } // ngày thanh toán thực tế thành công

    public Guid? PaymentMethodId { get; set; } // khóa ngoại đến PaymentMethod
    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}