using System.ComponentModel; // Import thư viện để dùng [DefaultValue]

public class RegisterRequestDTO
{
    [DefaultValue("a@gmail.com")]
    public string Email { get; set; } = "a@gmail.com";

    [DefaultValue("Anonymous User")]
    public string FullName { get; set; } = "Anonymous User";

    [DefaultValue("1@")]
    public string Password { get; set; } = "1@"; // Mặc định đặt mật khẩu

    [DefaultValue("0909090909")]
    public string? PhoneNumber { get; set; } = "0909090909";

    [DefaultValue(true)]
    public bool? Gender { get; set; } = true; // Mặc định là Nam

    [DefaultValue("2006-01-01")]
    public DateTime? DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-18); // Mặc định 18 tuổi

    [DefaultValue("123 Main Street, City, Country")]
    public string? Address { get; set; } = "123 Main Street, City, Country";

    [DefaultValue("https://img.freepik.com/free-psd/3d-illustration-human-avatar-profile_23-2150671142.jpg")]
    public string? ImageUrl { get; set; } = "https://img.freepik.com/free-psd/3d-illustration-human-avatar-profile_23-2150671142.jpg";
}