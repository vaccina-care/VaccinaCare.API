namespace VaccinaCare.Domain.DTOs.AuthDTOs
{
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
