﻿using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Child
{
    public int ChildId { get; set; }

    public int? ParentId { get; set; }

    public string? FullName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? MedicalHistory { get; set; }

    public virtual ICollection<PackageProgress> PackageProgresses { get; set; } = new List<PackageProgress>();

    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();
}