using System.Text.Json.Serialization;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.VaccineDTOs;

public class CreateVaccineDto
{
    public string? VaccineName { get; set; }
    public string? Description { get; set; }
    [JsonIgnore] 
    public string? PicUrl { get; set; }
    public string? Type { get; set; }
    public decimal? Price { get; set; }
    public int RequiredDoses { get; set; }
    public int DoseIntervalDays { get; set; }
    public BloodType? ForBloodType { get; set; }
    public bool? AvoidChronic { get; set; }
    public bool? AvoidAllergy { get; set; }
    public bool? HasDrugInteraction { get; set; }
    public bool? HasSpecialWarning { get; set; }
}