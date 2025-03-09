using System.ComponentModel;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.ChildDTOs;

public class CreateChildDto
{
    [DefaultValue("Hoàng minh tiến")] public string FullName { get; set; } = "Hoàng minh tiến";

    [DefaultValue("2022-01-01")] public DateOnly DateOfBirth { get; set; } = new(2022, 1, 1);

    [DefaultValue(true)] public bool Gender { get; set; } = true;

    [DefaultValue("No known medical issues")]
    public string? MedicalHistory { get; set; } = "No known medical issues";

    [DefaultValue(BloodType.Unknown)] public BloodType BloodType { get; set; } = BloodType.Unknown;

    [DefaultValue(false)] public bool HasChronicIllnesses { get; set; } = false;

    [DefaultValue(null)] public string? ChronicIllnessesDescription { get; set; } = null;

    [DefaultValue(false)] public bool HasAllergies { get; set; } = false;

    [DefaultValue(null)] public string? AllergiesDescription { get; set; } = null;

    [DefaultValue(false)] public bool HasRecentMedication { get; set; } = false;

    [DefaultValue(null)] public string? RecentMedicationDescription { get; set; } = null;

    [DefaultValue(false)] public bool HasOtherSpecialCondition { get; set; } = false;

    [DefaultValue(null)] public string? OtherSpecialConditionDescription { get; set; } = null;
}