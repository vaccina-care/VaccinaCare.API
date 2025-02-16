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
        private readonly IGenericRepository<VaccinePackage> _vaccinePackageRepository;
        private readonly IGenericRepository<VaccinePackageDetail> _vaccinePackageDetailRepository;
        private readonly IGenericRepository<VaccineSuggestion> _vaccineSuggestionRepository;
        public UnitOfWork(VaccinaCareDbContext dbContext, IGenericRepository<Notification> notificationRepository,
            IGenericRepository<User> userRepository, IGenericRepository<Role> roleRepository,
            IGenericRepository<Vaccine> vaccineRepository, IGenericRepository<Child> childRepository, IGenericRepository<Appointment> appointmentRepository, IGenericRepository<VaccinePackage> vaccinePackageRepository, IGenericRepository<VaccinePackageDetail> vaccinePackageDetailRepository, IGenericRepository<VaccineSuggestion> vaccineSuggestionRepository)
        {
            _dbContext = dbContext;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _vaccineRepository = vaccineRepository;
            _childRepository = childRepository;
            _appointmentRepository = appointmentRepository;
            _vaccinePackageRepository = vaccinePackageRepository;
            _vaccinePackageDetailRepository = vaccinePackageDetailRepository;
            _vaccineSuggestionRepository = vaccineSuggestionRepository;
        }
        public IGenericRepository<Notification> NotificationRepository => _notificationRepository;
        public IGenericRepository<User> UserRepository => _userRepository;
        public IGenericRepository<Child> ChildRepository => _childRepository;
        public IGenericRepository<Role> RoleRepository => _roleRepository;
        public IGenericRepository<Vaccine> VaccineRepository => _vaccineRepository;
        public IGenericRepository<Appointment> AppointmentRepository => _appointmentRepository;
        public IGenericRepository<VaccinePackage> VaccinePackageRepository => _vaccinePackageRepository;
        public IGenericRepository<VaccinePackageDetail> VaccinePackageDetailRepository => _vaccinePackageDetailRepository;
        public IGenericRepository<VaccineSuggestion> VaccineSuggestionRepository => _vaccineSuggestionRepository;
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