using Microsoft.EntityFrameworkCore;

namespace VaccinaCare.Domain
{
    public class VaccinaCareDbContext : DbContext
    {
        public VaccinaCareDbContext(DbContextOptions<VaccinaCareDbContext> options) : base(options) { }

    }
}
