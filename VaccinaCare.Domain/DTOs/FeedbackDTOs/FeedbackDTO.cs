namespace VaccinaCare.Domain.DTOs.FeedbackDTOs;

public class FeedbackDTO
{
    public Guid AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comments { get; set; }
}