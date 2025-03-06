using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;

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
    public virtual DbSet<AppointmentsVaccine> AppointmentsServices { get; set; }
    public virtual DbSet<CancellationPolicy> CancellationPolicies { get; set; }
    public virtual DbSet<Child> Children { get; set; }
    public virtual DbSet<Feedback> Feedbacks { get; set; }
    public virtual DbSet<VaccineIntervalRules> VaccineIntervalRules { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<PackageProgress> PackageProgresses { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Vaccine> Vaccines { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UsersVaccination> UsersVaccinationServices { get; set; }
    public virtual DbSet<VaccinationRecord> VaccinationRecords { get; set; }
    public virtual DbSet<VaccinePackage> VaccinePackages { get; set; }
    public virtual DbSet<VaccinePackageDetail> VaccinePackageDetails { get; set; }
    public virtual DbSet<VaccineSuggestion> VaccineSuggestions { get; set; }

    #endregion


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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

        // Map tables to match entity names

        #region ToTable

        modelBuilder.Entity<Appointment>().ToTable(nameof(Appointment));
        modelBuilder.Entity<AppointmentsVaccine>().ToTable(nameof(AppointmentsVaccine));
        modelBuilder.Entity<CancellationPolicy>().ToTable(nameof(CancellationPolicy));
        modelBuilder.Entity<Child>().ToTable(nameof(Child));
        modelBuilder.Entity<Feedback>().ToTable(nameof(Feedback));
        modelBuilder.Entity<Invoice>().ToTable(nameof(Invoice));
        modelBuilder.Entity<Notification>().ToTable(nameof(Notification));
        modelBuilder.Entity<PackageProgress>().ToTable(nameof(PackageProgress));
        modelBuilder.Entity<Payment>().ToTable(nameof(Payment));
        modelBuilder.Entity<Role>().ToTable(nameof(Role));
        modelBuilder.Entity<Vaccine>().ToTable(nameof(Vaccine));
        modelBuilder.Entity<User>().ToTable(nameof(User));
        modelBuilder.Entity<UsersVaccination>().ToTable(nameof(UsersVaccination));
        modelBuilder.Entity<VaccinationRecord>().ToTable(nameof(VaccinationRecord));
        modelBuilder.Entity<VaccinePackage>().ToTable(nameof(VaccinePackage));
        modelBuilder.Entity<VaccinePackageDetail>().ToTable(nameof(VaccinePackageDetail));
        modelBuilder.Entity<VaccineSuggestion>().ToTable(nameof(VaccineSuggestion));

        #endregion

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__8ECDFCA28A60492C");
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CancellationReason).HasColumnType("text");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.VaccineType).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(255);

            entity.HasOne(e => e.CancellationPolicies) // Một Appointment có một CancellationPolicy
                .WithMany(p => p.Appointments) // Một CancellationPolicy có nhiều Appointment
                .HasForeignKey(e => e.PolicyId) // Khóa ngoại PolicyId trong Appointment
                .OnDelete(DeleteBehavior.Restrict); // Không xóa Appointment khi xóa Policy (tùy chọn)

            entity.HasMany(a => a.Feedbacks) // Một Appointment có nhiều Feedback
                .WithOne(f => f.Appointment) // Một Feedback thuộc về một Appointment
                .HasForeignKey(f => f.AppointmentId) // Khóa ngoại AppointmentId
                .OnDelete(DeleteBehavior.Cascade); // Xóa Feedback khi xóa Appointment

            entity.HasOne(a => a.Child) // Một Appointment thuộc về một Child
                .WithMany(c => c.Appointments) // Một Child có nhiều Appointment
                .HasForeignKey(a => a.ChildId) // Khóa ngoại ChildId trong Appointment
                .OnDelete(DeleteBehavior.Restrict); // Hành vi khi xóa Child
        });

        modelBuilder.Entity<VaccineIntervalRules>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VaccineIntervalRules");

            entity.Property(e => e.MinIntervalDays)
                .IsRequired();

            entity.Property(e => e.CanBeGivenTogether)
                .IsRequired();

            entity.HasOne(e => e.Vaccine)
                .WithMany()
                .HasForeignKey(e => e.VaccineId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.RelatedVaccine)
                .WithMany()
                .HasForeignKey(e => e.RelatedVaccineId)
                .OnDelete(DeleteBehavior.NoAction);
        });


        modelBuilder.Entity<Appointment>()
            .Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.VaccineType)
            .HasConversion<string>()
            .HasMaxLength(50);

        modelBuilder.Entity<AppointmentVaccineSuggestions>()
            .HasKey(av => new { av.AppointmentId, av.VaccineSuggestionId });

        modelBuilder.Entity<AppointmentVaccineSuggestions>()
            .HasOne(av => av.Appointment)
            .WithMany(a => a.AppointmentVaccineSuggestions)
            .HasForeignKey(av => av.AppointmentId);

        modelBuilder.Entity<AppointmentVaccineSuggestions>()
            .HasOne(av => av.VaccineSuggestion)
            .WithMany(vs => vs.AppointmentVaccineSuggestions)
            .HasForeignKey(av => av.VaccineSuggestionId);


        modelBuilder.Entity<AppointmentsVaccine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3B38F27673DFA862");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 0)");
            entity.HasOne(d => d.Appointment)
                .WithMany(p => p.AppointmentsVaccines)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Appointme__Appoi__5BE2A6F2");

            entity.HasOne(d => d.Vaccine)
                .WithMany(p => p.AppointmentsVaccines)
                .HasForeignKey(d => d.VaccineId)
                .HasConstraintName("FK__Appointme__Servi__5CD6CB2B");
        });

        modelBuilder.Entity<CancellationPolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cancella__2E1339442FD3D154");

            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)") // Hỗ trợ Unicode cho mô tả
                .IsRequired();

            entity.Property(e => e.PenaltyFee)
                .HasColumnType(
                    "decimal(18, 2)") // Giữ nguyên kiểu decimal, nhưng sửa thành 2 chữ số thập phân để chính xác hơn
                .IsRequired();

            entity.Property(e => e.PolicyName)
                .HasColumnType("nvarchar(255)") // Đảm bảo hỗ trợ tiếng Việt
                .IsRequired();
        });


        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Children__BEFA0736DBF1AE94");

            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(255);
            entity.Property(e => e.MedicalHistory).HasColumnType("text");

            entity.HasOne(e => e.Parent) // Tham chiếu đến User (phụ huynh)
                .WithMany(u => u.Children) // Một User có nhiều Child
                .HasForeignKey(e => e.ParentId) // Khóa ngoại ParentId trong Child
                .OnDelete(DeleteBehavior.Cascade); // Hành vi khi xóa User (tùy chọn)


            entity.HasMany(c => c.VaccinationRecords) // Một Child có nhiều VaccinationRecord
                .WithOne(v => v.Child) // Một VaccinationRecord thuộc về một Child
                .HasForeignKey(v => v.ChildId) // Khóa ngoại ChildId
                .OnDelete(DeleteBehavior.Cascade); // Xóa VaccinationRecord khi xóa Child
        });

        modelBuilder.Entity<Child>()
            .Property(c => c.BloodType)
            .HasConversion(
                v => v.ToString(), // Convert Enum to String for Database
                v => (BloodType)Enum.Parse(typeof(BloodType), v) // Convert String to Enum when reading
            );

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Users");

            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);

            entity.Property(e => e.RoleName) // Lưu RoleType trực tiếp
                .HasConversion<string>() // Nếu muốn lưu dưới dạng string (hoặc xóa dòng này để lưu int)
                .IsRequired();

            entity.HasMany(u => u.Children) // Một User có nhiều Child
                .WithOne(c => c.Parent) // Một Child có một Parent
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.PackageProgresses) // Một User có nhiều PackageProgress
                .WithOne(p => p.Parent)
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RoleName)
                .HasConversion(
                    v => v.ToString(),
                    v => (RoleType)Enum.Parse(typeof(RoleType), v)
                )
                .IsRequired();
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
            entity.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<PackageProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PackageProgress");
            entity.HasOne(e => e.Parent)
                .WithMany(u => u.PackageProgresses) // Một User có nhiều PackageProgresses
                .HasForeignKey(e => e.ParentId) // Khóa ngoại ParentId
                .OnDelete(DeleteBehavior.Restrict); // Không tự động xóa PackageProgress khi xóa User
        });


        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__9B556A587D9A7EB1");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PaymentStatus).HasMaxLength(255);
        });

        modelBuilder.Entity<Vaccine>()
            .Property(v => v.ForBloodType)
            .HasConversion(
                v => v.ToString(),
                v => (BloodType)Enum.Parse(typeof(BloodType), v)
            );

        modelBuilder.Entity<VaccineSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VaccineS__940995287532A581");
            entity.Property(e => e.SuggestedVaccine).HasColumnType("text");
            entity.Property(e => e.Status).HasMaxLength(255);
        });

        modelBuilder.Entity<VaccineSuggestion>()
            .HasOne(vs => vs.Child)
            .WithMany(c => c.VaccineSuggestions)
            .HasForeignKey(vs => vs.ChildId)
            .OnDelete(DeleteBehavior.Cascade); // Enables cascading delete


        modelBuilder.Entity<VaccinationRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VaccinationRecords");

            entity.Property(e => e.VaccinationDate).IsRequired(false);
            entity.Property(e => e.ReactionDetails).HasColumnType("text").IsRequired(false);

            entity.HasOne(e => e.Child)
                .WithMany(c => c.VaccinationRecords)
                .HasForeignKey(e => e.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Vaccine)
                .WithMany(v => v.VaccinationRecords)
                .HasForeignKey(e => e.VaccineId)
                .OnDelete(DeleteBehavior.Restrict); // or Cascade if you want dependent records to be deleted
        });

        modelBuilder.Entity<VaccinePackageDetail>()
            .HasOne(vpd => vpd.Package)
            .WithMany(vp => vp.VaccinePackageDetails)
            .HasForeignKey(vpd => vpd.PackageId)
            .OnDelete(DeleteBehavior.Cascade); // Enable cascading delete
    }
}