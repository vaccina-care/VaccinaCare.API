namespace VaccinaCare.Domain.Entities;

public partial class User : BaseEntity
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool? Gender { get; set; } 
    public DateTime? DateOfBirth { get; set; } 
    public string? ImageUrl { get; set; } 
    public string? PhoneNumber { get; set; }
    public string? PasswordHash { get; set; }
    public int? RoleId { get; set; }
    
    
    public virtual Role? Role { get; set; } // Tham chiếu đến Role
    public virtual ICollection<Child> Children { get; set; } = new List<Child>(); 
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<PackageProgress> PackageProgresses { get; set; } = new List<PackageProgress>();
    public virtual ICollection<UsersVaccination> UsersVaccinations { get; set; } = new List<UsersVaccination>();
}

