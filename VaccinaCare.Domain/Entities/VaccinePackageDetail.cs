namespace VaccinaCare.Domain.Entities;

public partial class VaccinePackageDetail : BaseEntity
{

    public int? PackageId { get; set; }

    public int? ServiceId { get; set; }

    public int? DoseOrder { get; set; }

    public virtual VaccinePackage? Package { get; set; }

    public virtual Service? Service { get; set; }
}
