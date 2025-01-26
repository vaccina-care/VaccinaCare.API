﻿using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.UserDTOs;

public class CurrentUserDTO
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ImageUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public RoleType? RoleName { get; set; } 
}
