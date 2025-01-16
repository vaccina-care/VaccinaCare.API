using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int? AppointmentId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }
}
