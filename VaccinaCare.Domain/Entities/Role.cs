using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.Entities;

public partial class Role : BaseEntity
{
    public RoleType RoleName { get; set; } 

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

