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
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IGenericRepository<Vaccine> _vaccineRepository;
        private readonly IGenericRepository<Child> _childRepository;
        private readonly IGenericRepository<Appointment> _appointmentRepository;
        public UnitOfWork(VaccinaCareDbContext dbContext, IGenericRepository<Notification> notificationRepository,
            IGenericRepository<User> userRepository, IGenericRepository<Role> roleRepository,
            IGenericRepository<Vaccine> vaccineRepository, IGenericRepository<Child> childRepository, IGenericRepository<Appointment> appointmentRepository)
        {
            _dbContext = dbContext;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _vaccineRepository = vaccineRepository;
            _childRepository = childRepository;
            _appointmentRepository = appointmentRepository;
        }
        public IGenericRepository<Notification> NotificationRepository => _notificationRepository;
        public IGenericRepository<User> UserRepository => _userRepository;
        public IGenericRepository<Child> ChildRepository => _childRepository;
        public IGenericRepository<Role> RoleRepository => _roleRepository;
        public IGenericRepository<Vaccine> VaccineRepository => _vaccineRepository;
        public IGenericRepository<Appointment> AppointmentRepository => _appointmentRepository;
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