using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaccinaCare.Repository.Interfaces;

public interface ICurrentTime
{
    public DateTime GetCurrentTime();
}