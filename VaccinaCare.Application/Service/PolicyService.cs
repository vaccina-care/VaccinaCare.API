using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.PolicyDTOs;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;
using VaccinaCare.Application.Interface.Common;
using Microsoft.EntityFrameworkCore;

namespace VaccinaCare.Application.Service;

public class PolicyService : IPolicyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _logger;

    public PolicyService(IUnitOfWork unitOfWork, ILoggerService logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PolicyDto> CreatePolicyAsync(CreatePolicyDto policyDto)
    {
        try
        {
            _logger.Info($"Starting policy creation. Name: {policyDto.PolicyName}");
            var policy = new CancellationPolicy
            {
                PolicyName = policyDto.PolicyName,
                Description = policyDto.Description,
                CancellationDeadline = policyDto.CancellationDeadline ?? 0,
                PenaltyFee = policyDto.PenaltyFee ?? 0
            };

            await _unitOfWork.CancellationPolicyRepository.AddAsync(policy);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Policy created successfully: {policy.PolicyName}");

            var policyDtoResult = new PolicyDto
            {
                PolicyId = policy.Id, 
                PolicyName = policy.PolicyName,
                Description = policy.Description,
                CancellationDeadline = policy.CancellationDeadline,
                PenaltyFee = policy.PenaltyFee
            };

            return policyDtoResult;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in CreatePolicyAsync {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeletePolicyAsync(Guid id)
    {
        try
        {
            _logger.Info($"Attempting to delete policy with ID:  {id}");

            var policy = await _unitOfWork.CancellationPolicyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.Warn($"Policy with ID {id} not found.");
                return false;
            }

            await _unitOfWork.CancellationPolicyRepository.SoftRemove(policy);
            await _unitOfWork.SaveChangesAsync();

            _logger.Info($"Successfully deleted policy with ID: {id}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while deleting policy {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<Pagination<PolicyDto>> GetAllPolicyAsync(PaginationParameter pagination)
    {
        try
        {
            _logger.Info(
                $"Fetching policy with pagination: Page {pagination.PageIndex}, Size {pagination.PageSize} ");

            var query = _unitOfWork.CancellationPolicyRepository.GetQueryable();

            var totalPolicies = await query.CountAsync();

            var policies = await query
                .OrderBy(f => f.PolicyName)
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            if (!policies.Any())
            {
                _logger.Warn($"No policys found on page {pagination.PageIndex}.");
                return new Pagination<PolicyDto>(new List<PolicyDto>(), 0, pagination.PageIndex,
                    pagination.PageSize);
            }

            _logger.Success($"Retrieved {policies.Count} feedbacks on page {pagination.PageIndex}");

            var policyDtos = policies.Select(policy => new PolicyDto
            {
                PolicyId = policy.Id,
                PolicyName = policy.PolicyName,
                Description = policy.Description,
                CancellationDeadline = policy.CancellationDeadline,
                PenaltyFee = policy.PenaltyFee
            }).ToList();

            return new Pagination<PolicyDto>(policyDtos, totalPolicies, pagination.PageIndex, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching policys: {ex.Message}");
            throw new Exception("An error occurred while fetching feedbacks. Please try again later");
        }
    }

    public async Task<PolicyDto> GetPolicyByIdAsync(Guid id)
    {
        try
        {
            _logger.Info($"Fetching policy with ID: {id}");

            var policy = await _unitOfWork.CancellationPolicyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.Warn($"Policy with ID {id} not found.");
                throw new KeyNotFoundException("Policy not found.");
            }

            _logger.Info($"Policy fethching successfully: {policy.PolicyName}");

            return new PolicyDto
            {
                PolicyId = policy.Id,
                PolicyName = policy.PolicyName,
                Description = policy.Description,
                CancellationDeadline = policy.CancellationDeadline,
                PenaltyFee = policy.PenaltyFee
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"[GetPolicyById] - Error: {ex.Message}");
            throw;
        }
    }

    public async Task<PolicyDto> UpdatePolicyAsync(Guid id, UpdatePolicyDto policyDto)
    {
        try
        {
            _logger.Info($"Updating policy with ID: {id}");

            var policy = await _unitOfWork.CancellationPolicyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                _logger.Warn($"Policy with ID {id} not found.");
                throw new KeyNotFoundException("Policy not found.");
            }

            var isChanged = false;

            if (!string.IsNullOrWhiteSpace(policyDto.PolicyName) && policy.PolicyName != policyDto.PolicyName)
            {
                policy.PolicyName = policyDto.PolicyName;
                isChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(policyDto.Description) && policy.Description != policyDto.Description)
            {
                policy.Description = policyDto.Description;
                isChanged = true;
            }

            if (policyDto.CancellationDeadline.HasValue &&
                policy.CancellationDeadline != policyDto.CancellationDeadline)
            {
                policy.CancellationDeadline = policyDto.CancellationDeadline.Value;
                isChanged = true;
            }

            if (policyDto.PenaltyFee.HasValue && policy.PenaltyFee != policyDto.PenaltyFee)
            {
                policy.PenaltyFee = policyDto.PenaltyFee.Value;
                isChanged = true;
            }

            if (isChanged)
            {
                await _unitOfWork.CancellationPolicyRepository.Update(policy);
                await _unitOfWork.SaveChangesAsync();
                _logger.Info($"Policy updated successfully: {policy.PolicyName}");
            }
            else
            {
                _logger.Info($"No changes detected for policy ID: {id}. Skipping update.");
            }

            return new PolicyDto
            {
                PolicyName = policy.PolicyName,
                Description = policy.Description,
                CancellationDeadline = policy.CancellationDeadline,
                PenaltyFee = policy.PenaltyFee
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while updating policy {id}: {ex.Message}");
            throw;
        }
    }
}