using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface
{
    public interface IAppointmentService
    {
        Task<CreateAppointmentDto> CreateAppointment(CreateAppointmentDto createAppointmentDto);

        Task<Pagination<CreateAppointmentDto>> GetAppointmentByParent(Guid parentId, PaginationParameter pagination);
        Task<CreateAppointmentDto> UpdateAppointment(Guid id, CreateAppointmentDto createAppointmentDto);
        Task<CreateAppointmentDto> DeleteAppointment(Guid id);
        Task<IEnumerable<AppointmentDTO>> GenerateAppointmentsForVaccines(Guid childId, List<Guid> selectedVaccineIds, DateTime startDate);
    }
}