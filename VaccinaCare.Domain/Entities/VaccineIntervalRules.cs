using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VaccinaCare.Domain.Entities;

public class VaccineIntervalRules : BaseEntity
{
    [Required]
    public Guid VaccineId { get; set; } // Vaccine chính

    public Guid? RelatedVaccineId { get; set; } // Vaccine có liên quan (nếu có)

    [Required]
    public int MinIntervalDays { get; set; } 

    [Required]
    public bool CanBeGivenTogether { get; set; } 

    [ForeignKey("VaccineId")]
    public virtual Vaccine? Vaccine { get; set; }

    [ForeignKey("RelatedVaccineId")]
    public virtual Vaccine? RelatedVaccine { get; set; }
}