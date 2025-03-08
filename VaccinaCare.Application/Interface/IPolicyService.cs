

using VaccinaCare.Domain.DTOs.PolicyDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface
{
     public interface IPolicyService
    {
        Task<PolicyDto> GetPolicyByIdAsync(Guid id);
        Task<PolicyDto> CreatePolicyAsync(PolicyDto policyDto);
        Task<PolicyDto> UpdatePolicyAsync(Guid id,PolicyDto policyDto);
        Task<bool> DeletePolicyAsync(Guid id);
        Task<Pagination<PolicyDto>> GetAllPolicyAsync(PaginationParameter pagination);  
    }
}
