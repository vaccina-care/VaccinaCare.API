namespace VaccinaCare.Domain.Entities;

public partial class PackageProgress : BaseEntity
{

    public Guid? ParentId { get; set; }

    public Guid? PackageId { get; set; }

    public Guid? ChildId { get; set; }

    public int? DosesCompleted { get; set; }

    public int? DosesRemaining { get; set; }

    public virtual Child? Child { get; set; }

    public virtual VaccinePackage? Package { get; set; }

    public virtual User? Parent { get; set; }
}
