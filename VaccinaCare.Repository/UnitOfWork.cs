using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly VaccinaCareDbContext _dbContext;
        private readonly IGenericRepository<Notification> _notificationRepository;
        private readonly IGenericRepository<User> _userRepository;

        public UnitOfWork(VaccinaCareDbContext dbContext, IGenericRepository<Notification> notificationRepository, IGenericRepository<User> userRepository)
        {
            _dbContext = dbContext;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
        }

        public IGenericRepository<Notification> NotificationRepository => _notificationRepository;
        public IGenericRepository<User> UserRepository => _userRepository;

        public Task<int> SaveChangesAsync()
        {
            try
            {
                return _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
