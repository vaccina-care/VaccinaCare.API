using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface
{
    public interface IAppointmentService
    {
        Task<Appointment?> GetAppointmentDetailsByChildIdAsync(Guid childId);
        Task<Pagination<CreateAppointmentDto>> GetAppointmentByParent(Guid parentId, PaginationParameter pagination);
    }
}