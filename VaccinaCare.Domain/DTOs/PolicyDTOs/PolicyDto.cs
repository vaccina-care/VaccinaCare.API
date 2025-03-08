﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaccinaCare.Domain.DTOs.PolicyDTOs
{
    public class PolicyDto
    {
        public string? PolicyName { get; set; }
        public string? Description { get; set; }
        public int? CancellationDeadline { get; set; }
        public decimal? PenaltyFee { get; set; }
    }
}
