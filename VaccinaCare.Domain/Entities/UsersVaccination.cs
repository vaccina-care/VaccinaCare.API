namespace VaccinaCare.Domain.Entities;

public class UsersVaccination : BaseEntity
{
    public Guid? UserId { get; set; }

    public Guid? ServiceId { get; set; }

    public virtual Vaccine? Service { get; set; }

    public virtual User? User { get; set; }
}