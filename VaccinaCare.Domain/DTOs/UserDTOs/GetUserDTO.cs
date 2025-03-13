using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.UserDTOs;

public class GetUserDTO
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public RoleType? RoleName { get; set; }
    public DateTime CreatedAt { get; set; }
}