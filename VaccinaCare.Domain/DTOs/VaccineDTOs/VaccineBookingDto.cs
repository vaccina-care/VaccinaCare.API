using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaccinaCare.Domain.DTOs.VaccineDTOs
{
   
        public class VaccineBookingDto
        {
            public Guid VaccineId { get; set; }
            public string VaccineName { get; set; }
            public int BookingCount { get; set; }
        }
}
