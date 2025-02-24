using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface;

public interface IChildService
{
    Task<ChildDto> CreateChildAsync(CreateChildDto childDto);
    Task<List<ChildDto>> GetChildrenByParentAsync();
    Task<ChildDto> UpdateChildrenAsync(Guid childId, UpdateChildDto childDto);
    Task DeleteChildrenByParentIdAsync(Guid childId);
}