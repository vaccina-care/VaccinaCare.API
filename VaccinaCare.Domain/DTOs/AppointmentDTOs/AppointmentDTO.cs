using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.AppointmentDTOs
{
    public class AppointmentDTO
    {
        public Guid Id { get; set; }
        public Guid? ChildId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public VaccineType VaccineType { get; set; }
        public List<Guid> VaccineIds { get; set; } = new List<Guid>();
    }
}
