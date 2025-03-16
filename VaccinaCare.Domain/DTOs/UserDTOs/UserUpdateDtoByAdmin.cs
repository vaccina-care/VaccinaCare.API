using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;

namespace VaccinaCare.Domain.DTOs.UserDTOs;

public class UserUpdateDtoByAdmin 
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}