using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<User> UserRepository { get; }
        IGenericRepository<Notification> NotificationRepository { get; }
        IGenericRepository<Vaccine> VaccineRepository { get; }
        Task<int> SaveChangesAsync();
    }

}