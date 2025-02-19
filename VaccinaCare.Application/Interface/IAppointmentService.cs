using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface
{
    public interface IAppointmentService
    {
        Task<IEnumerable<Appointment>> GenerateAppointmentsForVaccines(Guid childId, List<Guid> selectedVaccineIds, DateTime startDate);
        Task<Appointment?> GetAppointmentDetailsByChildIdAsync(Guid childId);
        Task<AppointmentDTO> BookConsultationAppointment(Guid childId, DateTime appointmentDate);
        Task<Pagination<CreateAppointmentDto>> GetAppointmentByParent(Guid parentId, PaginationParameter pagination);
    }
}