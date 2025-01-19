namespace VaccinaCare.Domain.Entities;

public partial class UsersVaccination : BaseEntity
{

    public int? UserId { get; set; }

    public int? ServiceId { get; set; }

    public virtual Vaccine? Service { get; set; }

    public virtual User? User { get; set; }
}
