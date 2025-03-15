using VaccinaCare.Domain.DTOs.PolicyDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface;

public interface IPolicyService
{
    Task<PolicyDto> CreatePolicyAsync(CreatePolicyDto policyDto);
    Task<PolicyDto> UpdatePolicyAsync(Guid id, UpdatePolicyDto policyDto);
    Task<bool> DeletePolicyAsync(Guid id);
    Task<Pagination<PolicyDto>> GetAllPolicyAsync(PaginationParameter pagination);
    Task<PolicyDto> GetPolicyByIdAsync(Guid id);
}