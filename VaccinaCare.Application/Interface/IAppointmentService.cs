using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface
{
    public interface IAppointmentService
    {
        Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(List<Guid> vaccineIds, Guid childId,
            Guid parentId, DateTime startDate);

        Task<Appointment?> GetAppointmentDetailsByChildIdAsync(Guid childId);
    }
}