using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace VaccinaCare.Domain.DTOs.UserDTOs;

public class UserUpdateDto
{
    public string? FullName { get; set; }
    public bool? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public IFormFile? ImageFile { get; set; }
    public string? ImageUrl { get; set; } // Add this to return the stored image URL
    public string? PhoneNumber { get; set; }
}