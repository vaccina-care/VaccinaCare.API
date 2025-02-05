using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface;

public interface IChildService
{
    Task<ChildDto> CreateChildAsync(CreateChildDto childDto);
}