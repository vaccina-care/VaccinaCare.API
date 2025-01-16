using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class PackageProgress
{
    public int ProgressId { get; set; }

    public int? ParentId { get; set; }

    public int? PackageId { get; set; }

    public int? ChildId { get; set; }

    public int? DosesCompleted { get; set; }

    public int? DosesRemaining { get; set; }

    public virtual Child? Child { get; set; }

    public virtual VaccinePackage? Package { get; set; }

    public virtual User? Parent { get; set; }
}
