namespace VaccinaCare.Domain.Entities;

public partial class PackageProgress : BaseEntity
{
    public Guid? ParentId { get; set; }

    public Guid? PackageId { get; set; }

    public Guid? ChildId { get; set; }

    public int DosesCompleted { get; set; } // Số mũi đã tiêm

    public int TotalDoses { get; set; } // Tổng số mũi cần tiêm trong gói

    public int DosesRemaining => TotalDoses - DosesCompleted; // Số mũi còn lại

    public virtual Child? Child { get; set; }

    public virtual VaccinePackage? Package { get; set; }

    public virtual User? Parent { get; set; }
}