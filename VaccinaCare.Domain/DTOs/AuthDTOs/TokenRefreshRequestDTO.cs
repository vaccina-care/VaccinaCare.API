namespace VaccinaCare.Domain.DTOs.AuthDTOs;

public class TokenRefreshRequestDTO
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}