using System.ComponentModel;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.ChildDTOs;

public class UpdateChildDto
{
    public string? FullName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [DefaultValue(true)] // Mặc định là Nam
    public bool Gender { get; set; } = true;

    public string? MedicalHistory { get; set; }

    [DefaultValue(Enums.BloodType.O)] // Mặc định nhóm máu O
    public BloodType? BloodType { get; set; }

    [DefaultValue(false)] // Mặc định là không có bệnh mãn tính
    public bool HasChronicIllnesses { get; set; } = false;

    public string? ChronicIllnessesDescription { get; set; }

    [DefaultValue(false)] // Không có dị ứng
    public bool HasAllergies { get; set; } = false;

    public string? AllergiesDescription { get; set; }

    [DefaultValue(false)] // Không dùng thuốc gần đây
    public bool HasRecentMedication { get; set; } = false;

    public string? RecentMedicationDescription { get; set; }

    [DefaultValue(false)] // Không có tình trạng đặc biệt
    public bool HasOtherSpecialCondition { get; set; } = false;

    public string? OtherSpecialConditionDescription { get; set; }
}