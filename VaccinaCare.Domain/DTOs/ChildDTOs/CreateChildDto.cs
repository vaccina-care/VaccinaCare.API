using System.ComponentModel;
using VaccinaCare.Domain.Enums; // Import thư viện cần thiết

public class CreateChildDto
{
    [DefaultValue("Nguyen Van A")]
    public string FullName { get; set; } = "Nguyen Van A"; // Tên mặc định

    [DefaultValue("2022-01-01")] 
    public DateOnly DateOfBirth { get; set; } = new DateOnly(2022, 1, 1); // Mặc định trẻ 2 tuổi

    [DefaultValue(true)]
    public bool Gender { get; set; } = true; // Mặc định là Nam

    [DefaultValue("No known medical issues")]
    public string? MedicalHistory { get; set; } = "No known medical issues"; // Không có tiền sử bệnh

    [DefaultValue(BloodType.Unknown)]
    public BloodType BloodType { get; set; } = BloodType.Unknown; // Nhóm máu không xác định

    [DefaultValue(false)]
    public bool HasChronicIllnesses { get; set; } = false; // Mặc định không có bệnh mãn tính

    [DefaultValue(null)]
    public string? ChronicIllnessesDescription { get; set; } = null;

    [DefaultValue(false)]
    public bool HasAllergies { get; set; } = false; // Mặc định không dị ứng

    [DefaultValue(null)]
    public string? AllergiesDescription { get; set; } = null;

    [DefaultValue(false)]
    public bool HasRecentMedication { get; set; } = false; // Không dùng thuốc gần đây

    [DefaultValue(null)]
    public string? RecentMedicationDescription { get; set; } = null;

    [DefaultValue(false)]
    public bool HasOtherSpecialCondition { get; set; } = false; // Không có triệu chứng đặc biệt

    [DefaultValue(null)]
    public string? OtherSpecialConditionDescription { get; set; } = null;
}