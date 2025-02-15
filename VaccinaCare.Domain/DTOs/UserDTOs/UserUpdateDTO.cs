namespace VaccinaCare.Domain.DTOs.UserDTOs;

public class UserUpdateDto
{
    public string? FullName { get; set; }

    public bool? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Address { get; set; }

    public string? ImageUrl { get; set; }

    public string? PhoneNumber { get; set; }
}
