namespace VaccinaCare.Domain.DTOs.NotificationDTOs;

public class NotificationForUserDTO
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public Guid? UserId { get; set; }
    public string? Role { get; set; }
    public bool IsRead { get; set; } = false;
}