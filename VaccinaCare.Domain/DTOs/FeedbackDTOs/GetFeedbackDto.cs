using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaccinaCare.Domain.DTOs.FeedbackDTOs;

public class GetFeedbackDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comments { get; set; }
}