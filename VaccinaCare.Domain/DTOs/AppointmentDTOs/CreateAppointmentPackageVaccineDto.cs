namespace VaccinaCare.Domain.DTOs.AppointmentDTOs;

public class CreateAppointmentPackageVaccineDto
{
    //package
    public Guid PackageId { get; set; } // ID của gói vaccine

    public Guid ChildId { get; set; }

    public DateTime StartDate { get; set; } // Ngày bắt đầu tiêm chủng
}