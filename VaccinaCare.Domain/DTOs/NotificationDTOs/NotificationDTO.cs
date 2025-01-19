namespace VaccinaCare.Domain.DTOs.NotificationDTOs;

public class NotificationDTO
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public int? UserId { get; set; }
    public string? Role { get; set; }
}