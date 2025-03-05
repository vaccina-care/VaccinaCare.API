using System.ComponentModel;

namespace VaccinaCare.Domain.DTOs.AuthDTOs;

public class LoginRequestDto
{
    [DefaultValue("a@gmail.com")]
    public string? Email { get; set; }
    [DefaultValue("1@")]
    public string? Password { get; set; }
}