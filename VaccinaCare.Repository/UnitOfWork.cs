﻿using VaccinaCare.Domain;
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

        public UnitOfWork(VaccinaCareDbContext dbContext, IGenericRepository<Notification> notificationRepository, IGenericRepository<User> userRepository, IGenericRepository<Role> roleRepository, IGenericRepository<Vaccine> vaccineRepository)
        {
            _dbContext = dbContext;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _vaccineRepository = vaccineRepository;
        }

        public IGenericRepository<Notification> NotificationRepository => _notificationRepository;
        public IGenericRepository<User> UserRepository => _userRepository;
        public IGenericRepository<Role> RoleRepository => _roleRepository;
        public IGenericRepository<Vaccine> VaccineRepository => _vaccineRepository;

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
