using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaccinaCare.Domain.DTOs.NotificationDTOs;

public class NotificationForAppointmentDTO
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public Guid? UserId { get; set; }
    public Guid AppointmentId { get; set; }
    public string? Role { get; set; }
    public bool IsRead { get; set; } = false;
}