using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace VaccinaCare.Domain.DTOs.AuthDTOs;

public class TokenRefreshRequestDTO
{
    public string RefreshToken { get; set; }
}