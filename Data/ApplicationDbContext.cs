using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Models.Entities;

namespace ZoomAttendance.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
    }
}