using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class UpdateAppointmentStatusRequest
{
    public AppointmentStatus NewStatus { get; set; }
    public string? CancellationReason { get; set; }
}
