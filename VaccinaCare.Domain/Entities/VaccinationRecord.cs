using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class VaccinationRecord
{
    public int RecordId { get; set; }

    public int? ChildId { get; set; }

    public DateTime? VaccinationDate { get; set; }

    public string? ReactionDetails { get; set; }
}
