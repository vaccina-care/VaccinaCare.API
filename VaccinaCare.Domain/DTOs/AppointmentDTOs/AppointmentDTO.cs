namespace VaccinaCare.Domain.DTOs.AppointmentDTOs
{
    public class AppointmentDTO
    {
        public Guid Id { get; set; }
        public Guid? ChildId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string Status { get; set; }
        public string VaccineType { get; set; }
        public List<Guid> VaccineIds { get; set; } = new List<Guid>();
    }
}
