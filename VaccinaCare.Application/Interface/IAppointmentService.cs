﻿using VaccinaCare.Domain.DTOs.AppointmentDTOs;

namespace VaccinaCare.Application.Interface;

public interface IAppointmentService
{
    Task<List<AppointmentDTO>> GenerateAppointmentsForSingleVaccine(CreateAppointmentSingleVaccineDto request,
        Guid parentId);

    Task<List<AppointmentDTO>> GenerateAppointmentsForPackageVaccine(CreateAppointmentPackageVaccineDto request,
        Guid parentId);

    Task<List<AppointmentDTO>> UpdateAppointmentDate(Guid appointmentId, DateTime newDate);

    Task<List<AppointmentDTO>> GetListlAppointmentsByChildIdAsync(Guid childId);

    Task<AppointmentDTO> GetAppointmentDetailsByIdAsync(Guid appointmentId);
}