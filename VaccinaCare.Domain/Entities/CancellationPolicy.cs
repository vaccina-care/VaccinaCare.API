using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class CancellationPolicy
{
    public int PolicyId { get; set; }

    public string? PolicyName { get; set; }

    public string? Description { get; set; }

    public int? CancellationDeadline { get; set; }

    public decimal? PenaltyFee { get; set; }
}
