using Microsoft.EntityFrameworkCore;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.Domain;

public partial class VaccinaCareDbContext : DbContext
{
    public VaccinaCareDbContext()
    {
    }

    public VaccinaCareDbContext(DbContextOptions<VaccinaCareDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentsService> AppointmentsServices { get; set; }

    public virtual DbSet<CancellationPolicy> CancellationPolicies { get; set; }

    public virtual DbSet<Child> Children { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PackageProgress> PackageProgresses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceAvailability> ServiceAvailabilities { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UsersVaccinationService> UsersVaccinationServices { get; set; }

    public virtual DbSet<VaccinationRecord> VaccinationRecords { get; set; }

    public virtual DbSet<VaccinePackage> VaccinePackages { get; set; }

    public virtual DbSet<VaccinePackageDetail> VaccinePackageDetails { get; set; }

    public virtual DbSet<VaccineSuggestion> VaccineSuggestions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__8ECDFCA21C61A137");

            entity.Property(e => e.Id).HasColumnName("AppointmentID");
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CancellationReason).HasColumnType("text");
            entity.Property(e => e.ChildId).HasColumnName("ChildID");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.PolicyId).HasColumnName("PolicyID");
            entity.Property(e => e.PreferredTimeSlot).HasMaxLength(255);
            entity.Property(e => e.Room).HasMaxLength(255);
            entity.Property(e => e.ServiceType).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(255);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<AppointmentsService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3B38F27613FAC7C2");

            entity.Property(e => e.Id).HasColumnName("AppointmentServiceID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentsServices)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Appointme__Appoi__5BE2A6F2");

            entity.HasOne(d => d.Service).WithMany(p => p.AppointmentsServices)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Appointme__Servi__5CD6CB2B");
        });

        modelBuilder.Entity<CancellationPolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cancella__2E13394436566048");

            entity.Property(e => e.Id).HasColumnName("PolicyID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.PenaltyFee).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PolicyName).HasMaxLength(255);
        });

        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Children__BEFA0736B495D423");

            entity.Property(e => e.Id).HasColumnName("ChildID");
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(255);
            entity.Property(e => e.MedicalHistory).HasColumnType("text");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__6A4BEDF6FC881C6D");

            entity.ToTable("Feedback");

            entity.Property(e => e.Id).HasColumnName("FeedbackID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.Comments).HasColumnType("text");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invoices__D796AAD503D01278");

            entity.Property(e => e.Id).HasColumnName("InvoiceID");
            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Payment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.PaymentId)
                .HasConstraintName("FK__Invoices__Paymen__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Invoices__UserID__60A75C0F");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__20CF2E321E0C5AD9");

            entity.Property(e => e.Id).HasColumnName("NotificationID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.ReadStatus).HasMaxLength(255);

            entity.HasOne(d => d.Appointment).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Notificat__Appoi__5FB337D6");
        });

        modelBuilder.Entity<PackageProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PackageP__BAE29C85940767EF");

            entity.ToTable("PackageProgress");

            entity.Property(e => e.Id).HasColumnName("ProgressID");
            entity.Property(e => e.ChildId).HasColumnName("ChildID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");

            entity.HasOne(d => d.Child).WithMany(p => p.PackageProgresses)
                .HasForeignKey(d => d.ChildId)
                .HasConstraintName("FK__PackagePr__Child__66603565");

            entity.HasOne(d => d.Package).WithMany(p => p.PackageProgresses)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK__PackagePr__Packa__656C112C");

            entity.HasOne(d => d.Parent).WithMany(p => p.PackageProgresses)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__PackagePr__Paren__6477ECF3");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__9B556A5845D2C0DA");

            entity.Property(e => e.Id).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.PaymentStatus).HasMaxLength(255);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__8AFACE3A24D2CA60");

            entity.Property(e => e.Id).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(255);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Services__C51BB0EAC48FC5F7");

            entity.Property(e => e.Id).HasColumnName("ServiceID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.PicUrl).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ServiceName).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(255);
        });

        modelBuilder.Entity<ServiceAvailability>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ServiceA__DA3979916362444E");

            entity.ToTable("ServiceAvailability");
            entity.Property(e => e.Id).HasColumnName("AvailabilityID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.TimeSlot).HasMaxLength(255);

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceAvailabilities)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__ServiceAv__Servi__5AEE82B9");
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__1788CCACCB8B101C");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534C392CBE6").IsUnique();

            entity.Property(e => e.Id).HasColumnName("UserID");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
        });

        modelBuilder.Entity<UsersVaccinationService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UsersVac__C737CAF988A390FD");

            entity.Property(e => e.Id).HasColumnName("UserServiceID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Service).WithMany(p => p.UsersVaccinationServices)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__UsersVacc__Servi__5EBF139D");

            entity.HasOne(d => d.User).WithMany(p => p.UsersVaccinationServices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UsersVacc__UserI__5DCAEF64");
        });

        modelBuilder.Entity<VaccinationRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vaccinat__FBDF78C9C662C0AA");

            entity.Property(e => e.Id).HasColumnName("RecordID");
            entity.Property(e => e.ChildId).HasColumnName("ChildID");
            entity.Property(e => e.ReactionDetails).HasColumnType("text");
            entity.Property(e => e.VaccinationDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VaccinePackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VaccineP__322035EC4EC08B28");

            entity.Property(e => e.Id).HasColumnName("PackageID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.PackageName).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<VaccinePackageDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VaccineP__A7D8258AB9F62DF6");

            entity.Property(e => e.Id).HasColumnName("PackageDetailID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Package).WithMany(p => p.VaccinePackageDetails)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK__VaccinePa__Packa__628FA481");

            entity.HasOne(d => d.Service).WithMany(p => p.VaccinePackageDetails)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__VaccinePa__Servi__6383C8BA");
        });

        modelBuilder.Entity<VaccineSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VaccineS__94099528E1251CFB");

            entity.Property(e => e.Id).HasColumnName("SuggestionID");
            entity.Property(e => e.ChildId).HasColumnName("ChildID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Status).HasMaxLength(255);
            entity.Property(e => e.SuggestedVaccine).HasColumnType("text");

            entity.HasOne(d => d.Child).WithMany(p => p.VaccineSuggestions)
                .HasForeignKey(d => d.ChildId)
                .HasConstraintName("FK__VaccineSu__Child__59063A47");

            entity.HasOne(d => d.Service).WithMany(p => p.VaccineSuggestions)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__VaccineSu__Servi__59FA5E80");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
