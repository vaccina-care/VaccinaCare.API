using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using VaccinaCare.Repository.Interfaces;
using VaccinaCare.Repository.Utils;

namespace VaccinaCare.Repository.Commons
{
    public class ClaimsService : IClaimsService
    {
        public ClaimsService(IHttpContextAccessor httpContextAccessor)
        {
            // todo implementation to get the current userId
            var identity = httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;
            var extractedId = AuthenTools.GetCurrentUserId(identity);
            GetCurrentUserId = string.IsNullOrEmpty(extractedId) ? Guid.Empty : Guid.Parse(extractedId);
            IpAddress = httpContextAccessor?.HttpContext?.Connection?.LocalIpAddress?.ToString();
        }

        public Guid GetCurrentUserId { get; }

        public string? IpAddress { get; }
    }
}