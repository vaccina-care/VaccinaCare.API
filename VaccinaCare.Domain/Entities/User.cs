using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public int? RoleId { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<PackageProgress> PackageProgresses { get; set; } = new List<PackageProgress>();

    public virtual ICollection<UsersVaccinationService> UsersVaccinationServices { get; set; } = new List<UsersVaccinationService>();
}
