namespace VaccinaCare.Domain.DTOs.PolicyDTOs;

public class CreatePolicyDto
{
    public string? PolicyName { get; set; }
    public string? Description { get; set; }
    public int? CancellationDeadline { get; set; }
    public decimal? PenaltyFee { get; set; }
}