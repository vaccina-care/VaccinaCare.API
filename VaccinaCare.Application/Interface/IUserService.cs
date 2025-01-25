using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Application.Interface
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
    }
}
