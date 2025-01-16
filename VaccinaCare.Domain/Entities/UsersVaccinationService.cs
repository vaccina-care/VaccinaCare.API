using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class UsersVaccinationService
{
    public int UserServiceId { get; set; }

    public int? UserId { get; set; }

    public int? ServiceId { get; set; }

    public virtual Service? Service { get; set; }

    public virtual User? User { get; set; }
}
