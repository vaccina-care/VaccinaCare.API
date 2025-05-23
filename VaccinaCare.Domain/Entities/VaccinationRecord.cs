﻿namespace VaccinaCare.Domain.Entities;

public class VaccinationRecord : BaseEntity
{
    public Guid ChildId { get; set; }
    public Guid VaccineId { get; set; }
    public DateTime? VaccinationDate { get; set; }
    public string? ReactionDetails { get; set; }
    public int DoseNumber { get; set; } // Số mũi đã tiêm
    public virtual Child? Child { get; set; }
    public virtual Vaccine? Vaccine { get; set; }
}