using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaccinaCare.Domain.DTOs.VaccineDTOs
{
    public class VaccineDTO
    {
        public string? VaccineName { get; set; }

        public string? Description { get; set; }

        public string? PicUrl { get; set; }

        public string? Type { get; set; }

        public decimal? Price { get; set; }
    }
}
