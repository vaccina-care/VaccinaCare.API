﻿namespace VaccinaCare.Domain.DTOs.AuthDTOs
{
    public class RegisterRequestDTO
    {
        public string Email { get; set; } = "default@gmail.com";
        public string FullName { get; set; } = "Skibidi Toilet";
        public string Password { get; set; }
        public string? PhoneNumber { get; set; } = "0909090909";
        public bool? Gender { get; set; } = true;
        public DateTime? DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-18);
        public string? Address { get; set; } = "Poor con quá, giá như con được sống ở USA thì tài năng của con đc tỏa sáng";
        public string? ImageUrl { get; set; } = "https://img.freepik.com/free-psd/3d-illustration-human-avatar-profile_23-2150671142.jpg";

    }
}
