namespace VaccinaCare.Domain.Entities;

public partial class VaccinePackageDetail : BaseEntity
{

    public Guid? PackageId { get; set; }

    public Guid? VaccineId { get; set; }

    public int? DoseOrder { get; set; }

    public virtual VaccinePackage? Package { get; set; }

    public virtual Vaccine? Service { get; set; }
}
