using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Repository;

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
    private readonly IGenericRepository<VaccinationRecord> _vaccinationRecordRepository;
    private readonly IGenericRepository<VaccineIntervalRules> _vaccineIntervalRules;
    private readonly IGenericRepository<AppointmentVaccineSuggestions> _appointmentVaccineSuggestionsRepository;
    private readonly IGenericRepository<AppointmentsVaccine> _appointmentsVaccineRepository;
    private readonly IGenericRepository<Feedback> _feedbackRepository;
    private readonly IGenericRepository<Payment> _paymentRepository;
    private readonly IGenericRepository<PaymentTransaction> _paymentTransactionRepository;
    private readonly IGenericRepository<Invoice> _invoiceRepository;
    private readonly IGenericRepository<CancellationPolicy> _cancellationPolicyRepository;

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
        IGenericRepository<PaymentTransaction> paymentTransactionRepository, IGenericRepository<CancellationPolicy> cancellationPolicyRepository)
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
        _vaccineIntervalRules = vaccineIntervalRules;
        _appointmentVaccineSuggestionsRepository = appointmentVaccineSuggestionsRepository;
        _appointmentsVaccineRepository = appointmentsVaccineRepository;
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _vaccinationRecordRepository = vaccinationRecordRepository;
        _feedbackRepository = feedbackRepository;
        _cancellationPolicyRepository = cancellationPolicyRepository;
    }

    public IGenericRepository<Notification> NotificationRepository => _notificationRepository;
    public IGenericRepository<User> UserRepository => _userRepository;
    public IGenericRepository<Child> ChildRepository => _childRepository;
    public IGenericRepository<Role> RoleRepository => _roleRepository;
    public IGenericRepository<Vaccine> VaccineRepository => _vaccineRepository;
    public IGenericRepository<Appointment> AppointmentRepository => _appointmentRepository;
    public IGenericRepository<VaccinePackage> VaccinePackageRepository => _vaccinePackageRepository;

    public IGenericRepository<VaccinePackageDetail> VaccinePackageDetailRepository =>
        _vaccinePackageDetailRepository;

    public IGenericRepository<VaccineSuggestion> VaccineSuggestionRepository => _vaccineSuggestionRepository;
    public IGenericRepository<VaccinationRecord> VaccinationRecordRepository => _vaccinationRecordRepository;
    public IGenericRepository<VaccineIntervalRules> VaccineIntervalRulesRepository => _vaccineIntervalRules;

    public IGenericRepository<AppointmentVaccineSuggestions> AppointmentVaccineSuggestionsRepository =>
        _appointmentVaccineSuggestionsRepository;

    public IGenericRepository<AppointmentsVaccine> AppointmentsVaccineRepository => _appointmentsVaccineRepository;
    public IGenericRepository<Feedback> FeedbackRepository => _feedbackRepository;
    public IGenericRepository<Payment> PaymentRepository => _paymentRepository;
    public IGenericRepository<PaymentTransaction> PaymentTransactionRepository => _paymentTransactionRepository;
    public IGenericRepository<Invoice> InvoiceRepository => _invoiceRepository;
    public IGenericRepository<CancellationPolicy> CancellationPolicyRepository => _cancellationPolicyRepository;

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