using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Role
{
    public int RoleId { get; set; }

    public string? RoleName { get; set; }
}
