﻿namespace VaccinaCare.Domain.DTOs.VaccineInternalRuleDTOs;

public class GetVaccineInternalRulesDto
{
    public Guid VaccineIntervalRUID { get; set; }
    public Guid VaccineId { get; set; }
    public Guid? RelatedVaccineId { get; set; }
    public int MinIntervalDays { get; set; }
    public bool CanBeGivenTogether { get; set; }
}