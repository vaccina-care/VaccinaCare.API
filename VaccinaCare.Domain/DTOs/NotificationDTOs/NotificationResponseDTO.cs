namespace VaccinaCare.Domain.DTOs.NotificationDTOs;

public class NotificationResponseDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Url { get; set; }
    public bool IsRead { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? AppointmentId { get; set; }
}