namespace VaccinaCare.Domain.Entities;

public partial class VaccineAvailability : BaseEntity
{

    public Guid? AvailabilityId { get; set; }

    public DateOnly? Date { get; set; }

    public string? TimeSlot { get; set; }

    public int? Capacity { get; set; }

    public int? Booked { get; set; }

    public virtual Vaccine? Vaccine { get; set; }
}
