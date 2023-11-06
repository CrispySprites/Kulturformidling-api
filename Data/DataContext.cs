using Kulturformidling_api.Data.Model;
using Microsoft.EntityFrameworkCore;
using Type = Kulturformidling_api.Data.Model.Type;

namespace Kulturformidling_api.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleRequest> RolesRequest { get; set; }
        public DbSet<Art> Art { get; set; }
        public DbSet<Type> Types { get; set; }
    }
}
