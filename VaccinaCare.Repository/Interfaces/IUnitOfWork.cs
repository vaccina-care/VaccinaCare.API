using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<User> UserRepository { get; }
        IGenericRepository<Child> ChildRepository { get; }
        IGenericRepository<Role> RoleRepository { get; }
        IGenericRepository<Notification> NotificationRepository { get; }
        IGenericRepository<Vaccine> VaccineRepository { get; }
        IGenericRepository<Appointment> AppointmentRepository { get; }
        IGenericRepository<VaccinePackage> VaccinePackageRepository { get; }
        IGenericRepository<VaccinePackageDetail> VaccinePackageDetailRepository { get; }
        Task<int> SaveChangesAsync();
    }

}