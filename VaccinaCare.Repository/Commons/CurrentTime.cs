using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Repository.Commons
{
    public class CurrentTime : ICurrentTime
    {
        public DateTime GetCurrentTime()
        {
            return DateTime.UtcNow.AddHours(7);
        }
    }
}