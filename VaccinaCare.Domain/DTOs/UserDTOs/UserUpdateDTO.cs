using System.ComponentModel.DataAnnotations;

namespace VaccinaCare.Domain.DTOs.UserDTOs;

public class UserUpdateDto
{
    [MaxLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string? FullName { get; set; } 

    public bool? Gender { get; set; }

    [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; } 

    [Url(ErrorMessage = "Invalid image URL format.")]
    public string? ImageUrl { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string? PhoneNumber { get; set; } 
}
