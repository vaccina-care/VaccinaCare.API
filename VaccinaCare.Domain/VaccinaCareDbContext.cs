using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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

    #region DbSet
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

    #endregion




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType != typeof(BaseEntity))
            {
                // Configure common properties for BaseEntity
                modelBuilder.Entity(entityType.ClrType)
                    .Property<DateTime>("CreatedAt")
                    .HasDefaultValueSql("GETUTCDATE()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property<bool>("IsDeleted")
                    .HasDefaultValue(false);

                // Dynamically set query filter for IsDeleted
                var method = typeof(ModelBuilder)
                    .GetMethods()
                    .First(m => m.Name == "Entity" && m.IsGenericMethod)
                    .MakeGenericMethod(entityType.ClrType);

                var entityBuilder = method.Invoke(modelBuilder, null);
                var hasQueryFilterMethod = entityBuilder.GetType()
                    .GetMethods()
                    .First(m => m.Name == "HasQueryFilter");

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);

                hasQueryFilterMethod.Invoke(entityBuilder, new object[] { lambda });
            }
        }


        // Entity-specific configurations
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__8ECDFCA28A60492C");
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CancellationReason).HasColumnType("text");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.PreferredTimeSlot).HasMaxLength(255);
            entity.Property(e => e.Room).HasMaxLength(255);
            entity.Property(e => e.ServiceType).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(255);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 0)");

            // Thiết lập quan hệ với CancellationPolicy
            entity.HasOne(e => e.CancellationPolicy) // Một Appointment có một CancellationPolicy
                .WithMany(p => p.Appointments) // Một CancellationPolicy có nhiều Appointment
                .HasForeignKey(e => e.PolicyId) // Khóa ngoại PolicyId trong Appointment
                .OnDelete(DeleteBehavior.Restrict); // Không xóa Appointment khi xóa Policy (tùy chọn)
        });

        modelBuilder.Entity<AppointmentsService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3B38F27673DFA862");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 0)");
            entity.HasOne(d => d.Appointment)
                .WithMany(p => p.AppointmentsServices)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Appointme__Appoi__5BE2A6F2");

            entity.HasOne(d => d.Service)
                .WithMany(p => p.AppointmentsServices)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Appointme__Servi__5CD6CB2B");
        });

        modelBuilder.Entity<CancellationPolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cancella__2E1339442FD3D154");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.PenaltyFee).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PolicyName).HasMaxLength(255);
        });

        modelBuilder.Entity<Child>(entity =>
        {
            // Thiết lập khóa chính
            entity.HasKey(e => e.Id).HasName("PK__Children__BEFA0736DBF1AE94");

            // Cấu hình thuộc tính
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(255);
            entity.Property(e => e.MedicalHistory).HasColumnType("text");

            // Thiết lập quan hệ với User
            entity.HasOne(e => e.Parent) // Tham chiếu đến User (phụ huynh)
                .WithMany(u => u.Children) // Một User có nhiều Child
                .HasForeignKey(e => e.ParentId) // Khóa ngoại ParentId trong Child
                .OnDelete(DeleteBehavior.Cascade); // Hành vi khi xóa User (tùy chọn)
        });

        modelBuilder.Entity<User>(entity =>
        {
            // Thiết lập khóa chính
            entity.HasKey(e => e.Id).HasName("PK__Users__123456789");

            // Cấu hình các thuộc tính
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);

            // Thiết lập quan hệ với Child
            entity.HasMany(u => u.Children) // Một User có nhiều Child
                .WithOne(c => c.Parent) // Một Child có một Parent
                .HasForeignKey(c => c.ParentId) // Khóa ngoại ParentId trong Child
                .OnDelete(DeleteBehavior.Cascade); // Hành vi khi xóa User

            // Thiết lập quan hệ với Role
            entity.HasOne(e => e.Role) // Một User có một Role
                .WithMany(r => r.Users) // Một Role có nhiều User
                .HasForeignKey(e => e.RoleId) // Khóa ngoại RoleId trong User
                .OnDelete(DeleteBehavior.Restrict); // Hành vi khi xóa Role
        });

        modelBuilder.Entity<Role>(entity =>
        {
            // Thiết lập khóa chính
            entity.HasKey(e => e.Id).HasName("PK__Roles__ABCDEF123");

            // Cấu hình các thuộc tính
            entity.Property(e => e.RoleName).HasMaxLength(255);
        });




        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__6A4BEDF6E20C695E");
            entity.Property(e => e.Comments).HasColumnType("text");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invoices__D796AAD560EDA138");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__20CF2E3258A2D1E2");
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.ReadStatus).HasMaxLength(255);
        });

        modelBuilder.Entity<PackageProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PackageP__BAE29C85C1B611E7");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__9B556A587D9A7EB1");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PaymentStatus).HasMaxLength(255);
        });



        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Services__C51BB0EAE3CBFA2E");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.PicUrl).HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ServiceName).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(255);
        });

        modelBuilder.Entity<VaccineSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VaccineS__940995287532A581");
            entity.Property(e => e.SuggestedVaccine).HasColumnType("text");
            entity.Property(e => e.Status).HasMaxLength(255);
        });
    }
}

