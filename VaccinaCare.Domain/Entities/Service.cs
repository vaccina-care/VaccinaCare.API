using System;
using System.Collections.Generic;

namespace VaccinaCare.Domain.Entities;

public partial class Service
{
    public int ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public string? Description { get; set; }

    public string? PicUrl { get; set; }

    public string? Type { get; set; }

    public decimal? Price { get; set; }

    public virtual ICollection<AppointmentsService> AppointmentsServices { get; set; } = new List<AppointmentsService>();

    public virtual ICollection<ServiceAvailability> ServiceAvailabilities { get; set; } = new List<ServiceAvailability>();

    public virtual ICollection<UsersVaccinationService> UsersVaccinationServices { get; set; } = new List<UsersVaccinationService>();

    public virtual ICollection<VaccinePackageDetail> VaccinePackageDetails { get; set; } = new List<VaccinePackageDetail>();

    public virtual ICollection<VaccineSuggestion> VaccineSuggestions { get; set; } = new List<VaccineSuggestion>();
}
