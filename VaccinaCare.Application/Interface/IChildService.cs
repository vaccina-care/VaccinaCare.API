using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.Application.Interface;

public interface IChildService
{
    Task<ChildDto> CreateChildAsync(CreateChildDto childDto);
    Task<Pagination<ChildDto>> GetChildrenByParentAsync(PaginationParameter pagination);
    Task<ChildDto> UpdateChildAsync(Guid childId, UpdateChildDto childDto);
    Task DeleteChildAsync(Guid childId);
}