using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Application.Interface;

public interface IAppointmentService
{
    Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentDto request, Guid parentId);
    Task<List<AppointmentDTO>> GetListlAppointmentsByChildIdAsync(Guid childId);
    Task<AppointmentDTO> GetAppointmentDetailsByChildIdAsync(Guid childId);
}