using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public class Notification : BaseEntity
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public bool IsRead { get; set; } = false;
    public NotificationType Type { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? Role { get; set; }
}