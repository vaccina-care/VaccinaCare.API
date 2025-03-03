using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface
{
    public interface IAppointmentService
    {
        Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentDto request, Guid parentId);
        Task<Appointment?> GetAppointmentDetailsByChildIdAsync(Guid childId);
    }
}