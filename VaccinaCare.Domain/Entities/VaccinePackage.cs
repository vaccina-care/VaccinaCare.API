namespace VaccinaCare.Domain.Entities;

public partial class VaccinePackage : BaseEntity
{

    public string? PackageName { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public virtual ICollection<PackageProgress> PackageProgresses { get; set; } = new List<PackageProgress>();

    public virtual ICollection<VaccinePackageDetail> VaccinePackageDetails { get; set; } = new List<VaccinePackageDetail>();
}
