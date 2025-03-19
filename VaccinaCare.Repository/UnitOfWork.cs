using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly VaccinaCareDbContext _dbContext;

    public UnitOfWork(VaccinaCareDbContext dbContext, IGenericRepository<Notification> notificationRepository,
        IGenericRepository<User> userRepository, IGenericRepository<Role> roleRepository,
        IGenericRepository<Vaccine> vaccineRepository, IGenericRepository<Child> childRepository,
        IGenericRepository<Appointment> appointmentRepository,
        IGenericRepository<VaccinePackage> vaccinePackageRepository,
        IGenericRepository<VaccinePackageDetail> vaccinePackageDetailRepository,
        IGenericRepository<VaccineSuggestion> vaccineSuggestionRepository,
        IGenericRepository<VaccineIntervalRules> vaccineIntervalRules,
        IGenericRepository<AppointmentVaccineSuggestions> appointmentVaccineSuggestionsRepository,
        IGenericRepository<VaccinationRecord> vaccinationRecordRepository,
        IGenericRepository<AppointmentsVaccine> appointmentsVaccineRepository,
        IGenericRepository<Feedback> feedbackRepository, IGenericRepository<Payment> paymentRepository,
        IGenericRepository<Invoice> invoiceRepository,
        IGenericRepository<PaymentTransaction> paymentTransactionRepository,
        IGenericRepository<CancellationPolicy> cancellationPolicyRepository)
    {
        _dbContext = dbContext;
        NotificationRepository = notificationRepository;
        UserRepository = userRepository;
        RoleRepository = roleRepository;
        VaccineRepository = vaccineRepository;
        ChildRepository = childRepository;
        AppointmentRepository = appointmentRepository;
        VaccinePackageRepository = vaccinePackageRepository;
        VaccinePackageDetailRepository = vaccinePackageDetailRepository;
        VaccineSuggestionRepository = vaccineSuggestionRepository;
        VaccineIntervalRulesRepository = vaccineIntervalRules;
        AppointmentVaccineSuggestionsRepository = appointmentVaccineSuggestionsRepository;
        AppointmentsVaccineRepository = appointmentsVaccineRepository;
        PaymentRepository = paymentRepository;
        InvoiceRepository = invoiceRepository;
        PaymentTransactionRepository = paymentTransactionRepository;
        VaccinationRecordRepository = vaccinationRecordRepository;
        FeedbackRepository = feedbackRepository;
        CancellationPolicyRepository = cancellationPolicyRepository;
    }

    public IGenericRepository<Notification> NotificationRepository { get; }

    public IGenericRepository<User> UserRepository { get; }

    public IGenericRepository<Child> ChildRepository { get; }

    public IGenericRepository<Role> RoleRepository { get; }

    public IGenericRepository<Vaccine> VaccineRepository { get; }

    public IGenericRepository<Appointment> AppointmentRepository { get; }

    public IGenericRepository<VaccinePackage> VaccinePackageRepository { get; }

    public IGenericRepository<VaccinePackageDetail> VaccinePackageDetailRepository { get; }

    public IGenericRepository<VaccineSuggestion> VaccineSuggestionRepository { get; }

    public IGenericRepository<VaccinationRecord> VaccinationRecordRepository { get; }

    public IGenericRepository<VaccineIntervalRules> VaccineIntervalRulesRepository { get; }

    public IGenericRepository<AppointmentVaccineSuggestions> AppointmentVaccineSuggestionsRepository { get; }

    public IGenericRepository<AppointmentsVaccine> AppointmentsVaccineRepository { get; }

    public IGenericRepository<Feedback> FeedbackRepository { get; }

    public IGenericRepository<Payment> PaymentRepository { get; }

    public IGenericRepository<PaymentTransaction> PaymentTransactionRepository { get; }

    public IGenericRepository<Invoice> InvoiceRepository { get; }

    public IGenericRepository<CancellationPolicy> CancellationPolicyRepository { get; }

    public Task<int> SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}