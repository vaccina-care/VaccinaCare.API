﻿using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Repository.Interfaces;

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
    IGenericRepository<VaccinationRecord> VaccinationRecordRepository { get; }
    IGenericRepository<VaccineSuggestion> VaccineSuggestionRepository { get; }
    IGenericRepository<VaccineIntervalRules> VaccineIntervalRulesRepository { get; }
    IGenericRepository<AppointmentVaccineSuggestions> AppointmentVaccineSuggestionsRepository { get; }
    IGenericRepository<AppointmentsVaccine> AppointmentsVaccineRepository { get; }
    IGenericRepository<Feedback> FeedbackRepository { get; }
    IGenericRepository<Payment> PaymentRepository { get; }
    IGenericRepository<PaymentTransaction> PaymentTransactionRepository { get; }
    IGenericRepository<Invoice> InvoiceRepository { get; }
    IGenericRepository<CancellationPolicy> CancellationPolicyRepository { get; }

    Task<int> SaveChangesAsync();
}