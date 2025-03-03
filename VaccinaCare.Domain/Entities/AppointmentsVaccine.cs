namespace VaccinaCare.Domain.Entities;

public partial class AppointmentsVaccine : BaseEntity
{
    public Guid? AppointmentId { get; set; }

    public Guid? VaccineId { get; set; }

    /// <summary>
    /// Vì 1 loại Vaccine cần 1 hoặc nhiều lần đi tiêm nên anh đổi từ Quantity -> DoseNumber
    /// </summary>
    public int? DoseNumber { get; set; } // lưu số mũi đang tiêm của lịch hẹn đó

    public decimal? TotalPrice { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Vaccine? Vaccine { get; set; }
}