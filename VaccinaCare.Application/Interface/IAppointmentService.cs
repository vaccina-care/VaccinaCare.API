using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Application.Interface;

public interface IAppointmentService
{
    Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentSingleVaccineDto request,
        Guid parentId);

    Task<List<AppointmentDTO>> GenerateAppointmentsForPackageVaccine(CreateAppointmentPackageVaccineDto request,
        Guid parentId);

    Task<(bool success, string message)> UpdateAppointmentDate(Guid appointmentId, DateTime newDate);
    Task<List<AppointmentDTO>> GetListlAppointmentsByChildIdAsync(Guid childId);
    Task<AppointmentDTO> GetAppointmentDetailsByIdAsync(Guid appointmentId);
}