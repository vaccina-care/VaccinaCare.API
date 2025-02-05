namespace VaccinaCare.Domain.DTOs.ChildDTOs;

public class ChildDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool Gender { get; set; }
    public string? MedicalHistory { get; set; }
    public string? BloodType { get; set; }
    public bool HasChronicIllnesses { get; set; }
    public string? ChronicIllnessesDescription { get; set; }
    public bool HasAllergies { get; set; }
    public string? AllergiesDescription { get; set; }
    public bool HasRecentMedication { get; set; }
    public string? RecentMedicationDescription { get; set; }
    public bool HasOtherSpecialCondition { get; set; }
    public string? OtherSpecialConditionDescription { get; set; }
}
