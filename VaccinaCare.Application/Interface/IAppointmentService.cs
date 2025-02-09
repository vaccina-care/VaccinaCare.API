using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface
{
    public interface IAppointmentService
    {
        Task<CreateAppointmentDto> CreateAppointment(CreateAppointmentDto createAppointmentDto);
        Task<Pagination<CreateAppointmentDto>> GetAppointmentByParent(PaginationParameter pagination);
        Task<CreateAppointmentDto> UpdateAppointment(Guid id, CreateAppointmentDto createAppointmentDto);
        Task<CreateAppointmentDto> DeleteAppointment(Guid id);
    }
}
